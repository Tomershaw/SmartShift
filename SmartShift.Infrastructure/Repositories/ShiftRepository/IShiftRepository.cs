using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Infrastructure.Repositories;

// ����� ��� - ����� ��� ����� �����
public sealed class WeekSnapshot
{
    // ��� ������ ����� �-� ���� �� ���� - Pending + Approved
    public Dictionary<Guid, int> DesiredThisWeek { get; } = new();
    // ��� ������� ����� �-� ���� �� ���� - Approved ����
    public Dictionary<Guid, int> ApprovedThisWeek { get; } = new();
    // ��� ������� ����� ������ ���� �� ����
    public Dictionary<Guid, int> ApprovedThisMonth { get; } = new();
    // ����� ����� �� ����� ������ ������
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
    Task<bool> RegisterEmployeeForShiftAsync(Guid shiftId,
        string userId,
        Guid tenantId,
        EmployeeShiftAvailability shiftArrivalType,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IEnumerable<ShiftRegistration>> GetPendingRegistrationsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken = default);


    //Task<IEnumerable<ShiftRegistration>> GetApprovedRegistrationsAsync(Guid tenantId, Guid shiftId, CancellationToken cancellationToken = default);

    Task<bool> ApproveShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> RejectShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetApprovedEmployeesForShiftAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> GetApprovedEmployeesCountAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default);

    // ����� ���� - ������ ����� ��� ����� ����� ������� ���� ��� ���
    Task<WeekSnapshot> GetWeekAssignmentsSnapshotAsync(Guid tenantId, DateTime pivot, CancellationToken cancellationToken = default);

    
    Task<IEnumerable<Shift>> GetShiftsByEmployeeAsync(Guid employeeId, Guid tenantId, CancellationToken cancellationToken = default);

}
