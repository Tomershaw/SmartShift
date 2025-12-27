using MediatR;
using Microsoft.Extensions.Logging;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Features.Scheduling.GetEmployeeShifts;

public class GetEmployeeShiftsQueryHandler : IRequestHandler<GetEmployeeShiftsQuery, GetEmployeeShiftsResult>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetEmployeeShiftsQueryHandler> _logger;

    public GetEmployeeShiftsQueryHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetEmployeeShiftsQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetEmployeeShiftsResult> Handle(GetEmployeeShiftsQuery request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId == Guid.Empty)
        {
            _logger.LogWarning("GetEmployeeShifts called with empty employee ID");
            return new GetEmployeeShiftsResult { Success = false, Message = "מזהה עובד לא תקין" };
        }

        try
        {
            var tenantId = _currentUserService.GetTenantId();

            // ודא שהעובד קיים בטננט
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == request.EmployeeId && e.TenantId == tenantId, cancellationToken);

            if (!employeeExists)
                return new GetEmployeeShiftsResult { Success = false, Message = "העובד לא נמצא" };

            // אזור זמן ישראל
            TimeZoneInfo ilTz;
            try { ilTz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time"); }
            catch { ilTz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem"); }

            // חישוב גבולות UTC שקולים לימים בישראל: [utcStart, utcEndExclusive)

            var startLocal = new DateTime(request.StartDate.Year, request.StartDate.Month, request.StartDate.Day, 0, 0, 0);
            var endLocal = new DateTime(request.EndDate.Year, request.EndDate.Month, request.EndDate.Day, 0, 0, 0).AddDays(1);

            var utcStart = new DateTimeOffset(startLocal, ilTz.GetUtcOffset(startLocal)).ToUniversalTime();
            var utcEndExclusive = new DateTimeOffset(endLocal, ilTz.GetUtcOffset(endLocal)).ToUniversalTime();

            // שליפת הרשמות בטווח לפי StartTime UTC
            // שליפת כל ההרשמות בטווח, ממוין כך שהאחרונה (UpdatedAt או RegisteredAt) בסוף
            var registrations = await _context.ShiftRegistrations
                .AsNoTracking()
                .Include(sr => sr.Shift)
                .Where(sr =>
                    sr.EmployeeId == request.EmployeeId &&
                    sr.TenantId == tenantId &&
                    sr.Shift != null &&
                    sr.Shift.StartTime >= utcStart &&
                    sr.Shift.StartTime < utcEndExclusive)
                .OrderBy(sr => sr.UpdatedAt ?? sr.RegisteredAt) // הכי חדש בסוף הרשימה
                .ToListAsync(cancellationToken);

            var shifts = registrations.Select(reg => new EmployeeShiftDto
            {
                ShiftId = reg.ShiftId.ToString(),
                StartTime = reg.Shift!.StartTime,
                ShiftName = reg.Shift.Name,
                Description = reg.Shift.Description,
                RequiredEmployeeCount = reg.Shift.RequiredEmployeeCount,
                MinimumEmployeeCount = reg.Shift.MinimumEmployeeCount,
                SkillLevelRequired = reg.Shift.SkillLevelRequired,
                RegistrationId = reg.Id.ToString(),
                RegisteredAt = reg.RegisteredAt,
                RegistrationStatus = reg.Status.ToString(),
                ShiftArrivalType = reg.ShiftArrivalType.ToString(),
                ReviewedAt = reg.ReviewedAt,
                ReviewComment = reg.ReviewComment
            }).ToList();


            _logger.LogInformation("Found {Count} regs for employee {EmployeeId} in {Start}..{End} (IL days)",
                shifts.Count, request.EmployeeId, request.StartDate, request.EndDate);

            return new GetEmployeeShiftsResult
            {
                Success = true,
                EmployeeId = request.EmployeeId,
                Shifts = shifts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shifts for employee {EmployeeId}", request.EmployeeId);
            return new GetEmployeeShiftsResult { Success = false, Message = "שגיאה בשליפת המשמרות" };
        }
    }

}
