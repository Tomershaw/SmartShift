using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Services;
using SmartShift.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SmartShift.Infrastructure.AI;

public sealed record AdminNotesScore(
  int Reliability,
  int SkillDepth,
  int Attitude)
{
    public double TotalWeight => Reliability * 0.4 + SkillDepth * 0.3 + Attitude * 0.3;
}

public class ShiftAssignmentAIService : IShiftAssignmentAIService
{
    private readonly Kernel _kernel;
    private readonly ILogger<ShiftAssignmentAIService> _logger;
    private readonly EmployeeShiftMatchingService _matchingService;
    private readonly ShiftScoringService _scoringService;
    private readonly IShiftRepository _shiftRepository;

    public ShiftAssignmentAIService(
      Kernel kernel,
      ILogger<ShiftAssignmentAIService> logger,
      EmployeeShiftMatchingService matchingService,
      ShiftScoringService scoringService,
      IShiftRepository shiftRepository)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
        _scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
    }

    public async Task<string> AnalyzeShiftRequirementsAsync(
    Shift shift,
    IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> people,
    CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Analyze shift {ShiftId} with accurate employee data", shift.Id);

            var employeesJson = JsonSerializer.Serialize(
              people.Select(p => new
              {
                  p.Emp.Id,
                  Name = $"{p.Emp.FirstName} {p.Emp.LastName}",
                  p.Emp.SkillLevel,
                  p.Emp.PriorityRating,
                  p.Emp.MaxShiftsPerWeek,
                  Availability = DescribeArrival(p.Arrival),
                  p.Emp.AdminNotes,
                  p.Emp.EmployeeNotes
              }),
              new JsonSerializerOptions { WriteIndented = true });

            var prompt = $"""
                You are an intelligent shift assignment system.

                Shift:
                - Time: {shift.StartTime.DateTime:yyyy-MM-dd HH:mm}
                - Required: {shift.RequiredEmployeeCount}
                - Minimum: {shift.MinimumEmployeeCount}
                - Minimum Early: {shift.MinimumEarlyEmployees}
                - Skill Required: {shift.SkillLevelRequired}
                - Description: {shift.Description}

                Assigned employees with their actual availability:
                {employeesJson}

                Guidance:
                - Employee with "Early" availability can also handle regular shifts.
                - Employee with "Regular" availability is regular-only.

                Analyze in Hebrew: evaluate the assignment quality, skills match, priority distribution, weekly balance, fairness, and whether Minimum Early requirement is met.
                """;

            var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            return response.GetValue<string>() ?? "לא ניתן לבצע ניתוח";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analyze failed for shift {ShiftId}", shift.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Employee>> GetRecommendedEmployeesAsync(
     Shift shift,
     IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> people,
     CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🚀 START GetRecommendedEmployees for shift {ShiftId}: Required={Req}, Min={Min}, MinEarly={MinEarly}",
            shift.Id, shift.RequiredEmployeeCount, shift.MinimumEmployeeCount, shift.MinimumEarlyEmployees);

        if (shift.RequiredEmployeeCount <= 0)
            return Enumerable.Empty<Employee>();

        // ========================================
        // STEP 1: Basic Compatibility Check
        // Filter employees who pass basic rules (Skill + Gender/Early)
        // This does NOT check Weekly Limit yet
        // ========================================
        var basicCompatible = people
            .Where(p => _matchingService.CanEmployeeWorkShift(p.Emp, shift, p.Arrival))
            .DistinctBy(p => p.Emp.Id)
            .ToList();

        _logger.LogInformation("📋 Passed basic checks (Skill + Gender/Early): {Count}", basicCompatible.Count);

        if (basicCompatible.Count == 0)
        {
            _logger.LogWarning("❌ No employees passed basic compatibility checks!");
            return Enumerable.Empty<Employee>();
        }

        // ========================================
        // STEP 2: Get Weekly Assignments Snapshot
        // Fetch DB data needed for Weekly Limit check
        // ========================================
        var tenantId = shift.TenantId ?? throw new InvalidOperationException("Shift must have tenant ID");
        var snapshot = await _shiftRepository.GetWeekAssignmentsSnapshotAsync(tenantId, shift.StartTime.DateTime, cancellationToken);

        // ========================================
        // STEP 3: Strict Mode Filtering
        // Apply ALL rules including Weekly Limit
        // ========================================
        var strictCompatible = new List<(Employee Emp, EmployeeShiftAvailability Arrival)>();

        foreach (var p in basicCompatible)
        {
            var assignedWeek = snapshot.ApprovedThisWeek.TryGetValue(p.Emp.Id, out var aw) ? aw : 0;

            // Check if employee passes ALL hard rules (including Weekly Limit)
            if (_scoringService.PassHardRules(p.Emp, shift, assignedWeek, p.Arrival))
            {
                strictCompatible.Add(p);
            }
            else
            {
                _logger.LogDebug(
                    "❌ STRICT: {Name} failed PassHardRules (likely weekly limit: {Assigned}/{Max})",
                    $"{p.Emp.FirstName} {p.Emp.LastName}",
                    assignedWeek,
                    Math.Min(p.Emp.MaxShiftsPerWeek, 5));
            }
        }

        _logger.LogInformation("✅ STRICT mode (all rules passed): {Count} compatible", strictCompatible.Count);

        // ========================================
        // STEP 4: Emergency Mode (Fallback Logic)
        // If not enough employees in strict mode, relax Weekly Limit constraint
        // ========================================
        var compatibleWithArrival = strictCompatible;

        if (strictCompatible.Count < shift.RequiredEmployeeCount)
        {
            var missing = shift.RequiredEmployeeCount - strictCompatible.Count;

            _logger.LogWarning(
                "🚨 EMERGENCY MODE ACTIVATED! Only {Count} in strict mode, need {Req}. Missing: {Missing}",
                strictCompatible.Count, shift.RequiredEmployeeCount, missing);

            // Find employees who were rejected due to Weekly Limit
            var rejected = basicCompatible
                .Where(p => !strictCompatible.Any(s => s.Emp.Id == p.Emp.Id))
                .ToList();

            _logger.LogWarning(
                "⚠️ Found {Count} rejected employees (failed Weekly Limit check)",
                rejected.Count);

            if (rejected.Count > 0)
            {
                _logger.LogInformation("📊 Scoring rejected employees to select best {Missing} for emergency...", missing);

                // Score rejected employees using AdminNotes
                var rejectedScored = new List<(Employee Emp, EmployeeShiftAvailability Arrival, double Score)>();

                foreach (var p in rejected)
                {
                    _logger.LogInformation(
                        "🔥 EMERGENCY: Evaluating {Name} | AdminNotes: {Notes}",
                        $"{p.Emp.FirstName} {p.Emp.LastName}",
                        string.IsNullOrWhiteSpace(p.Emp.AdminNotes) ? "[EMPTY]" : $"'{p.Emp.AdminNotes}'");

                    // Get AdminNotes score
                    var adminScore = await GetAdminNotesScoreAsync(p.Emp.AdminNotes, cancellationToken);

                    // Get assignment data for scoring
                    var assignedWeek = snapshot.ApprovedThisWeek.TryGetValue(p.Emp.Id, out var aw) ? aw : 0;
                    var assignedMonth = snapshot.ApprovedThisMonth.TryGetValue(p.Emp.Id, out var am) ? am : 0;
                    var desiredWeek = snapshot.DesiredThisWeek.TryGetValue(p.Emp.Id, out var dw) ? dw : 0;

                    // Calculate full score
                    var s = _scoringService.CalculateScore(
                        p.Emp, shift, assignedWeek, assignedMonth,
                        snapshot.TeamMedianMonthly, desiredWeek,
                        (double)adminScore.Reliability,
                        (double)adminScore.SkillDepth,
                        (double)adminScore.Attitude
                    );

                    rejectedScored.Add((p.Emp, p.Arrival, s.Total));

                    _logger.LogInformation(
                        "🔥 EMERGENCY: {Name} | Score={Score:F2} (AdminBoost={Boost:F2})",
                        $"{p.Emp.FirstName} {p.Emp.LastName}",
                        s.Total, s.AdminBoost);
                }

                // Select best rejected employees based on score
                var bestRejected = rejectedScored
                    .OrderByDescending(x => x.Score)
                    .Take(missing)
                    .Select(x => (x.Emp, x.Arrival))
                    .ToList();

                _logger.LogInformation(
                    "✅ EMERGENCY: Adding {Count} best rejected employees to fill the gap",
                    bestRejected.Count);

                // Combine strict employees + emergency employees
                compatibleWithArrival = strictCompatible.Concat(bestRejected).ToList();

                _logger.LogInformation(
                    "📋 RELAXED mode result: {Strict} strict + {Added} emergency = {Total} total",
                    strictCompatible.Count, bestRejected.Count, compatibleWithArrival.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ No rejected employees available for emergency mode!");
            }
        }

        var compatible = compatibleWithArrival.Select(p => p.Emp).ToList();

        // ========================================
        // STEP 5: Check if There's Competition
        // If compatible <= required, no need for AdminNotes scoring
        // ========================================
        if (compatible.Count <= shift.RequiredEmployeeCount)
        {
            _logger.LogInformation(
                "✨ No competition ({Count} <= {Req}), returning all employees sorted by priority",
                compatible.Count, shift.RequiredEmployeeCount);

            return compatible
                .OrderByDescending(e => e.PriorityRating)
                .ThenByDescending(e => e.SkillLevel)
                .ThenBy(e => e.Id)
                .ToList();
        }

        // ========================================
        // STEP 6: Competition Exists - Score with AdminNotes
        // Calculate detailed scores for all compatible employees
        // ========================================
        _logger.LogInformation(
            "📊 Competition detected! Starting detailed AdminNotes scoring for {Count} candidates...",
            compatibleWithArrival.Count);

        var scored = new List<(Employee Emp, ShiftScoringService.ScoreBreakdown Score)>();

        foreach (var p in compatibleWithArrival)
        {
            var assignedWeek = snapshot.ApprovedThisWeek.TryGetValue(p.Emp.Id, out var aw) ? aw : 0;
            var assignedMonth = snapshot.ApprovedThisMonth.TryGetValue(p.Emp.Id, out var am) ? am : 0;
            var desiredWeek = snapshot.DesiredThisWeek.TryGetValue(p.Emp.Id, out var dw) ? dw : 0;

            _logger.LogInformation(
                "🎯 Processing Employee {Name} (ID: {Id}) | AdminNotes: {Notes}",
                $"{p.Emp.FirstName} {p.Emp.LastName}",
                p.Emp.Id,
                string.IsNullOrWhiteSpace(p.Emp.AdminNotes) ? "[EMPTY]" : $"'{p.Emp.AdminNotes}'");

            // Get AI-based AdminNotes score
            var adminScore = await GetAdminNotesScoreAsync(p.Emp.AdminNotes, cancellationToken);

            _logger.LogInformation(
                "📊 Employee {Name} (ID: {Id}) | AdminScores → R={R}, S={S}, A={A}, Weight={W:F2}",
                $"{p.Emp.FirstName} {p.Emp.LastName}", p.Emp.Id,
                adminScore.Reliability, adminScore.SkillDepth, adminScore.Attitude, adminScore.TotalWeight);

            // Calculate comprehensive score
            var s = _scoringService.CalculateScore(
                p.Emp, shift, assignedWeek, assignedMonth,
                snapshot.TeamMedianMonthly, desiredWeek,
                (double)adminScore.Reliability,
                (double)adminScore.SkillDepth,
                (double)adminScore.Attitude
            );

            _logger.LogInformation(
                "✅ Employee {Name} (ID: {Id}) | FinalScore={Total:F2} (AdminBoost={Boost:F2})",
                $"{p.Emp.FirstName} {p.Emp.LastName}", p.Emp.Id, s.Total, s.AdminBoost);

            scored.Add((Emp: p.Emp, Score: s));
        }

        if (scored.Count == 0)
        {
            _logger.LogWarning("⚠️ No employees passed scoring phase!");
            return Enumerable.Empty<Employee>();
        }

        _logger.LogInformation("📈 Successfully scored {Count} employees", scored.Count);

        // ========================================
        // STEP 7: Create Top Candidate Bucket
        // Select top performers for AI consideration
        // ========================================
        var topCount = Math.Min(
            compatible.Count,
            Math.Max(shift.RequiredEmployeeCount * 2, shift.RequiredEmployeeCount + 2));

        var topBucket = scored
            .OrderByDescending(x => x.Score.Total)
            .ThenByDescending(x => x.Emp.PriorityRating)
            .ThenByDescending(x => x.Emp.SkillLevel)
            .ThenBy(x => x.Emp.Id)
            .Select(x => x.Emp)
            .Take(topCount)
            .ToList();

        _logger.LogInformation(
            "🎯 Top bucket created with {Count} employees (needed {Needed})",
            topBucket.Count, shift.RequiredEmployeeCount);

        // Log top 5 candidates for debugging
        var top5 = scored
            .OrderByDescending(x => x.Score.Total)
            .Take(5)
            .Select((x, i) => $"#{i + 1}: {x.Emp.FirstName} {x.Emp.LastName} (Score: {x.Score.Total:F2}, Boost: {x.Score.AdminBoost:F2})")
            .ToList();

        _logger.LogInformation("🏆 Top 5 Candidates:\n{Top5}", string.Join("\n", top5));

        // ========================================
        // STEP 8: AI-Based Final Selection
        // Let AI choose from top bucket
        // ========================================
        List<Employee> aiList;
        try
        {
            var aiPick = await GetAIRecommendationsAsync(shift, topBucket, cancellationToken);
            aiList = aiPick.DistinctBy(e => e.Id).ToList();
            _logger.LogInformation("🤖 AI selected {Count} employees", aiList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI selection failed. Falling back to topBucket.");
            aiList = topBucket;
        }

        // Ensure we have enough employees
        if (aiList.Count < shift.RequiredEmployeeCount)
        {
            var add = topBucket
                .Where(e => aiList.All(p => p.Id != e.Id))
                .Take(shift.RequiredEmployeeCount - aiList.Count);

            aiList.AddRange(add);
            _logger.LogInformation("➕ Added {Count} more employees to reach requirement", add.Count());
        }

        // ========================================
        // STEP 9: Final Selection
        // Return exactly the required number of employees
        // ========================================
        var finalList = aiList
            .DistinctBy(e => e.Id)
            .Take(shift.RequiredEmployeeCount)
            .ToList();

        _logger.LogInformation(
            "🏁 FINAL selection: {Count} employees for shift {ShiftId}",
            finalList.Count, shift.Id);

        return finalList;
    }

    public async Task<string> GenerateShiftSummaryAsync(
    Shift shift,
    IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> assignedPeople,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var assigned = assignedPeople.DistinctBy(p => p.Emp.Id).ToList();

            var earlyCount = assigned.Count(p => p.Arrival == EmployeeShiftAvailability.Early);
            var regularCount = assigned.Count(p => p.Arrival == EmployeeShiftAvailability.Regular);
            var meetsEarlyRequirement = earlyCount >= shift.MinimumEarlyEmployees;

            var israelTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
            var local = TimeZoneInfo.ConvertTime(shift.StartTime, israelTz);
            var localDate = local.ToString("dd.MM.yyyy");
            var localTime = local.ToString("HH:mm");

            var assignedJson = JsonSerializer.Serialize(
        assigned.Select(p => new
        {
            Id = p.Emp.Id,
            Name = $"{p.Emp.FirstName} {p.Emp.LastName}",
            SkillLevel = p.Emp.SkillLevel,
            PriorityRating = p.Emp.PriorityRating,
            MaxShiftsPerWeek = p.Emp.MaxShiftsPerWeek,
            ArrivalType = p.Arrival == EmployeeShiftAvailability.Early ? "Early" : "Regular",
            AdminNotes = p.Emp.AdminNotes,
            EmployeeNotes = p.Emp.EmployeeNotes
        }),
        new JsonSerializerOptions { WriteIndented = true });

            var prompt = $"""
                You are an intelligent scheduling system generating a concise operational summary for a single shift.
                The prompt is in English, but your output MUST be entirely in Hebrew.
                Do not invent or assume data that is not explicitly provided.

                === SHIFT FACTS ===
                - Date: {localDate}
                - Time: {localTime}
                - Description: {shift.Description}
                - Required: {shift.RequiredEmployeeCount} | Minimum: {shift.MinimumEmployeeCount} | Minimum Early: {shift.MinimumEarlyEmployees}
                - Assigned: {assigned.Count} (Early: {earlyCount}, Regular: {regularCount})
                - Meets Minimum Early: {(meetsEarlyRequirement ? "Yes" : "No")}
                - Skill Required: {shift.SkillLevelRequired}

                === ASSIGNED EMPLOYEES (JSON) ===
                {assignedJson}

                === OUTPUT REQUIREMENTS ===
                - Output language: Hebrew only.
                - The summary must be short, factual, and structured.
                - Use the following exact structure:
                  1) Short title line with date, time, and general coverage status.
                  2) Staffing: one sentence describing required vs minimum vs actual assigned.
                  3) Early vs Regular: one sentence stating if Minimum Early is met and if there is any buffer.
                  4) Skills and priorities: one sentence summarizing the mix using only SkillLevel and PriorityRating fields.
                  5) One short operational insight (if relevant) about staffing balance or potential risk.
                  6) Reasons for selection: a short bullet list explaining WHY each employee was selected.
                     Each bullet must follow this format:
                       • <Name> - a concise factual reason based ONLY on: ArrivalType, fit to SkillLevelRequired,
                         PriorityRating, MaxShiftsPerWeek (workload suitability), and any relevant AdminNotes or EmployeeNotes.
                - Do NOT add extra sections, headers, or explanations.
                - Keep up to 6 short sentences before the bullet list.
                - Use 3 to 8 bullets maximum.
                - Be professional, concise, and data-driven.
                """;

            var resp = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            return resp.GetValue<string>() ?? "No summary generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Summary failed for shift {ShiftId}", shift.Id);
            return "שגיאה ביצירת תקציר";
        }
    }

    // ------------ Helpers ------------
    private async Task<IEnumerable<Employee>> GetAIRecommendationsAsync(
    Shift shift,
    List<Employee> candidates,
    CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(
          candidates.Select((e, i) => new
          {
              Index = i,
              e.Id,
              Name = $"{e.FirstName} {e.LastName}",
              e.SkillLevel,
              e.PriorityRating,
              e.MaxShiftsPerWeek
          }),
          new JsonSerializerOptions { WriteIndented = true });

        var prompt = $"""
            You are picking employees for a single shift.

            Shift:
            - Time: {shift.StartTime:yyyy-MM-dd HH:mm}
            - Required: {shift.RequiredEmployeeCount}
            - Minimum: {shift.MinimumEmployeeCount}
            - Minimum Early: {shift.MinimumEarlyEmployees}
            - Skill Required: {shift.SkillLevelRequired}

            Candidates (pre-filtered/scored):
            {json}

            Pick the best {shift.RequiredEmployeeCount} indexes.
            Rules: fit, skill, priority, fairness; try to satisfy Minimum Early if candidate set allows.
            Return ONLY a JSON array of indexes, e.g. [0,2,4].
            """;

        var resp = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var raw = resp.GetValue<string>()?.Trim() ?? "[]";

        var indexes = ParseIndexArraySafely(raw)
          .Where(i => i >= 0 && i < candidates.Count)
          .Distinct()
          .ToList();

        if (indexes.Count == 0) return Enumerable.Empty<Employee>();
        return indexes.Select(i => candidates[i]).DistinctBy(e => e.Id).ToList();
    }

    private static string DescribeArrival(EmployeeShiftAvailability a)
      => a == EmployeeShiftAvailability.Early ? "Early (can cover regular as well)" : "Regular only";

    private static List<int> ParseIndexArraySafely(string text)
    {
        try
        {
            var arr = JsonSerializer.Deserialize<List<int>>(text);
            if (arr != null) return arr;
        }
        catch
        {
            // fallback
        }

        return Regex.Matches(text, @"\d+")
          .Select(m => int.Parse(m.Value))
          .ToList();
    }

    private async Task<AdminNotesScore> GetAdminNotesScoreAsync(
    string adminNotes,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminNotes))
        {
            _logger.LogDebug("📝 AdminNotesScore: No admin notes - using default (5,5,5)");
            return new AdminNotesScore(5, 5, 5);
        }

        try
        {
            var safeNotes = adminNotes.Replace("\"", "'");

            _logger.LogInformation("🔍 AdminNotesScore: Analyzing notes → '{Notes}'", safeNotes);

            var prompt =
                "You are an HR expert analyzing staff notes. Your task is to rate an employee based on the provided admin note.\n" +
                $"Note: \"{safeNotes}\"\n\n" +
                "Rate the employee on a scale of 1 (Poor/High Risk) to 10 (Excellent/Asset) for the following criteria:\n" +
                "1. Reliability: Consistency, Punctuality, and commitment.\n" +
                "2. SkillDepth: Proficiency and mastery of the job role.\n" +
                "3. Attitude: Positivity, teamwork ability, and problem-solving skills.\n\n" +
                "Return ONLY a JSON object, strictly following this schema:\n" +
                "{ \"reliability\": 5, \"skill_depth\": 5, \"attitude\": 5 }";

            _logger.LogDebug("🤖 AdminNotesScore: Sending request to AI...");

            var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);

            var jsonText = response.GetValue<string>()?.Trim() ?? "{}";

            _logger.LogInformation("📥 AdminNotesScore: AI Response → {Json}", jsonText);

            var rawScore = JsonSerializer.Deserialize<AdminNotesScore>(
                jsonText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (rawScore != null)
            {
                _logger.LogDebug(
                    "✨ AdminNotesScore: Parsed → R={R}, S={S}, A={A}",
                    rawScore.Reliability, rawScore.SkillDepth, rawScore.Attitude);

                var finalScore = new AdminNotesScore(
                    Reliability: Math.Clamp(rawScore.Reliability, 1, 10),
                    SkillDepth: Math.Clamp(rawScore.SkillDepth, 1, 10),
                    Attitude: Math.Clamp(rawScore.Attitude, 1, 10)
                );

                _logger.LogInformation(
                    "✅ AdminNotesScore: Final → R={R}, S={S}, A={A} (Weight={W:F2})",
                    finalScore.Reliability, finalScore.SkillDepth, finalScore.Attitude, finalScore.TotalWeight);

                return finalScore;
            }

            _logger.LogError("❌ AdminNotesScore: Failed to parse JSON → {Json}", jsonText);
            return new AdminNotesScore(5, 5, 5);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ AdminNotesScore: JSON error → Notes='{Notes}'", adminNotes);
            return new AdminNotesScore(5, 5, 5);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AdminNotesScore: Unexpected error → Notes='{Notes}'", adminNotes);
            return new AdminNotesScore(5, 5, 5);
        }
    }
}