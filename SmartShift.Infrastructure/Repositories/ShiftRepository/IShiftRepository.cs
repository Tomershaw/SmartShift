using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Infrastructure.Repositories;

// מחלקת עזר - צילום מצב שבועי מרוכז
public sealed class WeekSnapshot
{
    // כמה הרשמות בשבוע א-ו עבור כל עובד - Pending + Approved
    public Dictionary<Guid, int> DesiredThisWeek { get; } = new();
    // כמה אישורים בשבוע א-ו עבור כל עובד - Approved בלבד
    public Dictionary<Guid, int> ApprovedThisWeek { get; } = new();
    // כמה אישורים בחודש הנוכחי עבור כל עובד
    public Dictionary<Guid, int> ApprovedThisMonth { get; } = new();
    // מדיאן חודשי של הצוות לאיזון הוגנות
    public int TeamMedianMonthly { get; init; } = 0;
}

public interface IShiftRepository
{
    Task<IEnumerable<Employee>> GetAllEmployeesForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Shift?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shift shift);
    Task<bool> RegisterEmployeeForShiftAsync(Guid shiftId, string userId,Guid tenantId, EmployeeShiftAvailability shiftArrivalType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftRegistration>> GetPendingRegistrationsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken = default);


    //Task<IEnumerable<ShiftRegistration>> GetApprovedRegistrationsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken = default);

    Task<bool> ApproveShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> RejectShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetApprovedEmployeesForShiftAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> GetApprovedEmployeesCountAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<WeekSnapshot> GetWeekAssignmentsSnapshotAsync(Guid tenantId, DateTimeOffset pivot, CancellationToken cancellationToken = default);
    Task<bool> CancelRegistrationAsync(Guid shiftId,string userId,Guid tenantId,CancellationToken cancellationToken = default);

    Task<IEnumerable<Shift>> GetShiftsByEmployeeAsync(Guid employeeId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsShiftOnDateAsync(Guid tenantId, DateTimeOffset date, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
}
 