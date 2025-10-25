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
        // 1) פירוש טווח תאריכים
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

            // 2) שליפת משמרות בטווח
            var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(start, end, request.TenantId, ct);
            if (!shifts.Any())
            {
                _logger.LogWarning("No shifts found for tenant {TenantId} in range {Start}..{End}",
                    request.TenantId, start, end);
                result.Message = "No shifts found in the specified date range.";
                return result;
            }

            // 3) עיבוד כל משמרת
            foreach (var shift in shifts.OrderBy(s => s.StartTime))
            {
                using (_logger.BeginScope(new Dictionary<string, object?>
                {
                    ["ShiftId"] = shift.Id,
                    ["ShiftStart"] = shift.StartTime
                }))
                {
                    var approvedCount = await _shiftRepository.GetApprovedEmployeesCountAsync(shift.Id, request.TenantId, ct);

                    var pendingRegs = (await _shiftRepository
                        .GetPendingRegistrationsAsync(request.TenantId, shift.Id, ct))
                        .ToList();

                    var required = shift.RequiredEmployeeCount;
                    var pendingCount = pendingRegs.Count;

                    // כמה חסר בכלל (כולל מאושרים שכבר קיימים)
                    var remainingNeeded = Math.Max(0, required - approvedCount);

                    // אם אין נרשמים או לא חסר — נחזיר סטטוס בלבד
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

                    // פיצול הרשמות לפי ה-ArrivalType של ההרשמה
                    var earlyRegs = pendingRegs.Where(r => r.ShiftArrivalType == EmployeeShiftAvailability.Early).ToList();
                    var regularRegs = pendingRegs.Where(r => r.ShiftArrivalType == EmployeeShiftAvailability.Regular).ToList();

                    var earlyEmployees = earlyRegs.Select(r => r.Employee!).Where(e => e != null).DistinctBy(e => e.Id).ToList();
                    var regularEmployees = regularRegs.Select(r => r.Employee!).Where(e => e != null).DistinctBy(e => e.Id).ToList();

                    // -------- מקור אמת אחד ל-Arrival לכל עובד --------
                    // אם לעובד יש כמה הרשמות, Early מנצח. זה מבטיח טייפ אחד לעובד.
                    var arrivalByEmployee = pendingRegs
                        .Where(r => r.Employee != null)
                        .GroupBy(r => r.Employee!.Id)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Any(r => r.ShiftArrivalType == EmployeeShiftAvailability.Early)
                                    ? EmployeeShiftAvailability.Early
                                    : EmployeeShiftAvailability.Regular
                        );

                    // לוקחים Employee אחד לכל Id
                    var employeesById = pendingRegs
                        .Where(r => r.Employee != null)
                        .GroupBy(r => r.Employee!.Id)
                        .ToDictionary(g => g.Key, g => g.First().Employee!);

                    // רשימת טאפלים בפורמט שה-AI מצפה לו
                    var peopleForAI = employeesById
                        .Select(kvp => (Emp: kvp.Value, Arrival: arrivalByEmployee[kvp.Key]))
                        .ToList();

                    // דירוג AI לכל הנרשמים (כדי לקבל סדר אחיד לשתי הקבוצות)
                    var aiOrdered = (await _aiService.GetRecommendedEmployeesAsync(shift, peopleForAI, ct)).ToList();

                    // מיפוי דירוג: EmployeeId -> אינדקס
                    var rank = aiOrdered.Select((e, i) => new { e.Id, i })
                                        .ToDictionary(x => x.Id, x => x.i);

                    // פונקציה מסדרת אוסף לפי דירוג ה-AI, מי שלא בדירוג נשלח לסוף
                    List<T> SortByAi<T>(IEnumerable<T> src, Func<T, Guid> key) =>
                        src.OrderBy(x => rank.TryGetValue(key(x), out var i) ? i : int.MaxValue).ToList();

                    earlyEmployees = SortByAi(earlyEmployees, e => e.Id);
                    regularEmployees = SortByAi(regularEmployees, e => e.Id);

                    // כמה Early נדרש מינימלית (אבל לא יותר ממה שחסר בפועל)
                    var earlyMin = shift.MinimumEarlyEmployees;
                    var earlyNeeded = Math.Min(earlyMin, remainingNeeded);

                    // בחירת Early
                    var planned = new List<Employee>();
                    var taken = new HashSet<Guid>();

                    foreach (var e in earlyEmployees)
                    {
                        if (planned.Count >= earlyNeeded) break;
                        if (taken.Add(e.Id))
                            planned.Add(e);
                    }

                    // השלמה לשאר הצורך מתוך מאגר משולב לפי דירוג AI
                    var completionPool = regularEmployees
                        .Concat(earlyEmployees.Where(e => !taken.Contains(e.Id)))
                        .Where(e => !taken.Contains(e.Id))
                        .ToList();

                    completionPool = SortByAi(completionPool, e => e.Id);

                    foreach (var e in completionPool)
                    {
                        if (planned.Count >= remainingNeeded) break;
                        if (taken.Add(e.Id))
                            planned.Add(e);
                    }

                    // שימוש חוזר במילון arrivalByEmployee להצגה וסיכום
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
                        startTime = new DateTimeOffset(DateTime.SpecifyKind(shift.StartTime, DateTimeKind.Utc)).ToString("o"), // ADD
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
    private static (DateTime start, DateTime endExclusive) ResolveRangeOrDefault(
        string? startStr, string? endStr, ILogger logger)
    {
        // ברירת מחדל: ראשון-חמישי של השבוע הבא (תאריכים בלי שעה)
        var nowUtc = DateTime.UtcNow;

        DateTime start;
        if (!DateTime.TryParse(startStr, out start))
        {
            int daysToNextSunday = ((int)DayOfWeek.Sunday - (int)nowUtc.DayOfWeek + 7) % 7;
            start = nowUtc.Date.AddDays(daysToNextSunday); // ראשון הבא 00:00
            logger.LogDebug("Start not provided/invalid. Using next Sunday {Start:yyyy-MM-dd}", start);
        }
        else
        {
            start = start.Date; // אם נתנו תאריך בלי שעה - ננרמל ל-00:00
        }

        DateTime end;
        if (!DateTime.TryParse(endStr, out end))
        {
            end = start.Date.AddDays(5); // חמישי הבא + 1 יום -> סוף-יום בלעדי
            logger.LogDebug("End not provided/invalid. Using Thursday end-exclusive {End:yyyy-MM-dd}", end);
        }
        else
        {
            // אם הגיע תאריך בלי שעה -> נעשה end-exclusive של היום הבא
            end = end.TimeOfDay == TimeSpan.Zero ? end.Date.AddDays(1) : end;
        }

        if (end <= start)
        {
            end = start.Date.AddDays(5); // end-exclusive של חמישי
            logger.LogWarning("End <= Start. Normalized endExclusive to {End:yyyy-MM-dd}", end);
        }

        return (start, end); // שים לב: end הוא end-exclusive
    }

}
