// SmartShift.Application/Features/ProcessShifts/ProcessShiftsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Infrastructure.AI;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.ProcessShifts;

public class ProcessShiftsCommandHandler
    : IRequestHandler<ProcessShiftsCommand, ProcessShiftsResult>
{
    private readonly IShiftAssignmentAIService _aiService;
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<ProcessShiftsCommandHandler> _logger;

    public ProcessShiftsCommandHandler(
        IShiftAssignmentAIService aiService,
        IShiftRepository shiftRepository,
        ILogger<ProcessShiftsCommandHandler> logger)
    {
        _aiService = aiService;
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public async Task<ProcessShiftsResult> Handle(ProcessShiftsCommand request, CancellationToken ct)
    {
        var (start, end) = ResolveRangeOrDefault(request.StartString, request.EndString, _logger);
        var result = new ProcessShiftsResult();

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["TenantId"] = request.TenantId,
            ["Start"] = start,
            ["End"] = end
        }))
        {
            _logger.LogInformation("Processing shifts. Tenant={TenantId}, Range={Start:yyyy-MM-dd}..{End:yyyy-MM-dd HH:mm}",
                request.TenantId, start, end);

            var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(start, end, request.TenantId, ct);
            if (!shifts.Any())
            {
                _logger.LogWarning("No shifts found for tenant {TenantId} in range {Start}..{End}",
                    request.TenantId, start, end);
                result.Message = "No shifts found in the specified date range.";
                return result;
            }

            foreach (var shift in shifts.OrderBy(s => s.StartTime))
            {
                using (_logger.BeginScope(new Dictionary<string, object?>
                {
                    ["ShiftId"] = shift.Id,
                    ["ShiftStart"] = shift.StartTime
                }))
                {
                    var approvedCount = await _shiftRepository.GetApprovedEmployeesCountAsync(shift.Id, request.TenantId, ct);
                    var pendingRegs = (await _shiftRepository.GetPendingRegistrationsAsync(request.TenantId, shift.Id, ct)).ToList();
                    var required = shift.RequiredEmployeeCount;
                    var pendingCount = pendingRegs.Count;
                    var remainingNeeded = Math.Max(0, required - approvedCount);

                    if (pendingCount == 0 || remainingNeeded == 0)
                    {
                        if (pendingCount == 0)
                            _logger.LogInformation("No pending regs. Approved={Approved}, Required={Required}", approvedCount, required);
                        else
                            _logger.LogInformation("Already satisfied. Approved={Approved}, Required={Required}", approvedCount, required);

                        result.Results.Add(new
                        {
                            shiftId = shift.Id,
                            required = shift.RequiredEmployeeCount,
                            minimum = shift.MinimumEmployeeCount,
                            minimumEarly = shift.MinimumEarlyEmployees,
                            approvedCount,
                            pendingCount,
                            remainingNeeded,
                            plannedCount = 0,
                            plannedEarlyCount = 0,
                            plannedRegularCount = 0,
                            meetsMinimumEarly = false
                        });
                        continue;
                    }

                    var arrivalByEmployee = pendingRegs
                        .Where(r => r.Employee != null)
                        .GroupBy(r => r.Employee!.Id)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Any(r => r.ShiftArrivalType == EmployeeShiftAvailability.Early)
                                    ? EmployeeShiftAvailability.Early
                                    : EmployeeShiftAvailability.Regular
                        );

                    var employeesById = pendingRegs
                        .Where(r => r.Employee != null)
                        .GroupBy(r => r.Employee!.Id)
                        .ToDictionary(g => g.Key, g => g.First().Employee!);

                    var peopleForAI = employeesById
                        .Select(kvp => (Emp: kvp.Value, Arrival: arrivalByEmployee[kvp.Key]))
                        .ToList();

                    var aiOrdered = (await _aiService.GetRecommendedEmployeesAsync(shift, peopleForAI, ct)).ToList();

                    var earlyIds = new HashSet<Guid>(
                        arrivalByEmployee
                            .Where(kvp => kvp.Value == EmployeeShiftAvailability.Early)
                            .Select(kvp => kvp.Key)
                    );

                    var regularIds = new HashSet<Guid>(
                        arrivalByEmployee
                            .Where(kvp => kvp.Value == EmployeeShiftAvailability.Regular)
                            .Select(kvp => kvp.Key)
                    );

                    var earlyEmployees = aiOrdered.Where(e => earlyIds.Contains(e.Id)).ToList();
                    var regularEmployees = aiOrdered.Where(e => regularIds.Contains(e.Id)).ToList();

                    var earlyMin = shift.MinimumEarlyEmployees;
                    var earlyNeeded = Math.Min(earlyMin, remainingNeeded);

                    var planned = new List<Employee>();
                    var taken = new HashSet<Guid>();

                    foreach (var e in earlyEmployees)
                    {
                        if (planned.Count >= earlyNeeded) break;
                        if (taken.Add(e.Id))
                            planned.Add(e);
                    }

                    var completionPool = regularEmployees
                        .Concat(earlyEmployees.Where(e => !taken.Contains(e.Id)))
                        .Where(e => !taken.Contains(e.Id))
                        .ToList();

                    foreach (var e in completionPool)
                    {
                        if (planned.Count >= remainingNeeded) break;
                        if (taken.Add(e.Id))
                            planned.Add(e);
                    }

                    var plannedWithArrival = planned
                        .Select(e => new
                        {
                            id = e.Id,
                            name = $"{e.FirstName} {e.LastName}",
                            skillLevel = e.SkillLevel,
                            priorityRating = e.PriorityRating,
                            arrivalType = arrivalByEmployee.TryGetValue(e.Id, out var at)
                                            ? (at == EmployeeShiftAvailability.Early ? "Early" : "Regular")
                                            : "Unknown"
                        })
                        .ToList();

                    var plannedEarlyCount = plannedWithArrival.Count(x => x.arrivalType == "Early");
                    var plannedRegularCount = plannedWithArrival.Count(x => x.arrivalType == "Regular");
                    var meetsMinimumEarly = plannedEarlyCount >= shift.MinimumEarlyEmployees;

                    var plannedCount = plannedWithArrival.Count;
                    var finalRemaining = Math.Max(0, required - approvedCount - plannedCount);

                    if (approvedCount + plannedCount < shift.MinimumEmployeeCount)
                        _logger.LogWarning("Below minimum after planning. Approved={Approved}, Planned={Planned}, Minimum={Minimum}",
                            approvedCount, plannedCount, shift.MinimumEmployeeCount);
                    else
                        _logger.LogInformation("Planned {Planned}. Approved={Approved}, Required={Required}, Pending={Pending}",
                            plannedCount, approvedCount, required, pendingCount);

                    if (!meetsMinimumEarly && earlyMin > 0)
                        _logger.LogWarning("Not enough Early employees. RequiredEarly={RequiredEarly}, PlannedEarly={PlannedEarly}, ShiftId={ShiftId}",
                            earlyMin, plannedEarlyCount, shift.Id);

                    var plannedPeople = planned.Select(e =>
                    {
                        var arrival = arrivalByEmployee.TryGetValue(e.Id, out var at)
                            ? at
                            : EmployeeShiftAvailability.Regular;
                        return (Emp: e, Arrival: arrival);
                    }).ToList();

                    var analysis = await _aiService.AnalyzeShiftRequirementsAsync(shift, plannedPeople, ct);
                    var summary = await _aiService.GenerateShiftSummaryAsync(shift, plannedPeople, ct);

                    result.Results.Add(new
                    {
                        shiftId = shift.Id,
                        startTime = shift.StartTime.ToString("o"),
                        required = shift.RequiredEmployeeCount,
                        minimum = shift.MinimumEmployeeCount,
                        minimumEarly = shift.MinimumEarlyEmployees,
                        approvedCount,
                        pendingCount,
                        remainingNeeded = finalRemaining,
                        plannedCount,
                        plannedEarlyCount,
                        plannedRegularCount,
                        meetsMinimumEarly,
                        planned = plannedWithArrival,
                        analysis,
                        summary
                    });
                }
            }

            _logger.LogInformation("Processing completed. ShiftsProcessed={Count}", result.Results.Count);
            return result;
        }
    }

    // ברירת מחדל לטווח: אם חסרים תאריכים → ראשון עד חמישי של השבוע הבא (UTC)
    // מחזיר startUtc (inclusive) ו-endExclusiveUtc (exclusive)
    private static (DateTimeOffset start, DateTimeOffset endExclusive) ResolveRangeOrDefault(
        string? startStr, string? endStr, ILogger logger)
    {
        // ברירת מחדל: ראשון-חמישי של השבוע הבא (תאריכים בלי שעה)
        var nowUtc = DateTimeOffset.UtcNow;

        DateTimeOffset start;
        if (!DateTimeOffset.TryParse(startStr, out start))
        {
            int daysToNextSunday = ((int)DayOfWeek.Sunday - (int)nowUtc.DayOfWeek + 7) % 7;
            start = nowUtc.Date.AddDays(daysToNextSunday); // ראשון הבא 00:00
            logger.LogDebug("Start not provided/invalid. Using next Sunday {Start:yyyy-MM-dd}", start);
        }
        else
        {
            start = new DateTimeOffset(start.Date, start.Offset); // אם נתנו תאריך בלי שעה - ננרמל ל-00:00
        }

        DateTimeOffset end;
        if (!DateTimeOffset.TryParse(endStr, out end))
        {
            end = start.Date.AddDays(5); // חמישי הבא + 1 יום -> סוף-יום בלעדי
            logger.LogDebug("End not provided/invalid. Using Thursday end-exclusive {End:yyyy-MM-dd}", end);
        }
        else
        {
            // אם הגיע תאריך בלי שעה -> נעשה end-exclusive של היום הבא
            end = end.TimeOfDay == TimeSpan.Zero ? new DateTimeOffset(end.Date.AddDays(1), end.Offset) : end;
        }

        if (end <= start)
        {
            end = new DateTimeOffset(start.Date.AddDays(5), start.Offset); // end-exclusive של חמישי
            logger.LogWarning("End <= Start. Normalized endExclusive to {End:yyyy-MM-dd}", end);
        }

        return (start, end); // שים לב: end הוא end-exclusive
    }
}