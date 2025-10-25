using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Services;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Infrastructure.AI;

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

    // ------------ 1) Analyze shift requirements (per-employee Arrival) ------------
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
                - Time: {shift.StartTime:yyyy-MM-dd HH:mm}
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

    // ------------ 2) Recommend employees (per-employee Arrival) ------------
    public async Task<IEnumerable<Employee>> GetRecommendedEmployeesAsync(
        Shift shift,
        IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> people,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recommend for shift {ShiftId}: Required={Req}, Min={Min}, MinEarly={MinEarly}",
            shift.Id, shift.RequiredEmployeeCount, shift.MinimumEmployeeCount, shift.MinimumEarlyEmployees);

        if (shift.RequiredEmployeeCount <= 0)
            return Enumerable.Empty<Employee>();

        // A) Hard filter by matching rules
        var compatible = people
            .Where(p => _matchingService.CanEmployeeWorkShift(p.Emp, shift, p.Arrival))
            .Select(p => p.Emp)
            .DistinctBy(e => e.Id)
            .ToList();

        if (compatible.Count == 0)
            return Enumerable.Empty<Employee>();

        if (compatible.Count <= shift.RequiredEmployeeCount)
            return compatible
                .OrderByDescending(e => e.PriorityRating)
                .ThenByDescending(e => e.SkillLevel)
                .ThenBy(e => e.Id)
                .ToList();

        // B) Fairness snapshot and scoring
        var tenantId = shift.TenantId ?? throw new InvalidOperationException("Shift must have tenant ID");
        var snapshot = await _shiftRepository.GetWeekAssignmentsSnapshotAsync(tenantId, shift.StartTime, cancellationToken);

        var scored = new List<(Employee Emp, ShiftScoringService.ScoreBreakdown Score)>();

        foreach (var e in compatible)
        {
            var assignedWeek = snapshot.ApprovedThisWeek.TryGetValue(e.Id, out var aw) ? aw : 0;
            var assignedMonth = snapshot.ApprovedThisMonth.TryGetValue(e.Id, out var am) ? am : 0;
            var desiredWeek = snapshot.DesiredThisWeek.TryGetValue(e.Id, out var dw) ? dw : 0;

            if (!_scoringService.PassHardRules(e, shift, assignedWeek))
                continue;

            var s = _scoringService.CalculateScore(
                e, shift, assignedWeek, assignedMonth, snapshot.TeamMedianMonthly, desiredWeek);

            scored.Add((Emp: e, Score: s));
        }

        if (scored.Count == 0)
            return Enumerable.Empty<Employee>();

        // C) Deterministic top bucket
        var topCount = Math.Min(compatible.Count, Math.Max(shift.RequiredEmployeeCount * 2, shift.RequiredEmployeeCount + 2));
        var topBucket = scored
            .OrderByDescending(x => x.Score.Total)
            .ThenByDescending(x => x.Emp.PriorityRating)
            .ThenByDescending(x => x.Emp.SkillLevel)
            .ThenBy(x => x.Emp.Id)
            .Select(x => x.Emp)
            .Take(topCount)
            .ToList();

        // D) Ask AI to choose from bucket
        List<Employee> aiList;
        try
        {
            var aiPick = await GetAIRecommendationsAsync(shift, topBucket, cancellationToken);
            aiList = aiPick.DistinctBy(e => e.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI selection failed. Falling back to topBucket.");
            aiList = topBucket;
        }

        if (aiList.Count < shift.RequiredEmployeeCount)
        {
            var add = topBucket.Where(e => aiList.All(p => p.Id != e.Id))
                               .Take(shift.RequiredEmployeeCount - aiList.Count);
            aiList.AddRange(add);
        }

        return aiList.DistinctBy(e => e.Id).Take(shift.RequiredEmployeeCount).ToList();
    }

    // ------------ 3) Generate shift summary (per-employee Arrival) ------------
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

            // Convert to Israel local time
            var israelTz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
            var local = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(shift.StartTime, DateTimeKind.Utc), israelTz);
            var localDate = local.ToString("dd.MM.yyyy");
            var localTime = local.ToString("HH:mm");

            // Prepare employee data
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

            // === Prompt in English, Output must be Hebrew ===
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
        catch { /* fallback */ }

        return Regex.Matches(text, @"\d+").Select(m => int.Parse(m.Value)).ToList();
    }
}
