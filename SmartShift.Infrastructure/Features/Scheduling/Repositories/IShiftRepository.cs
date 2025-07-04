using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.ShiftRegistrations; // הוסף את זה

namespace SmartShift.Infrastructure.Features.Scheduling.Repositories;

public interface IShiftRepository
{
    // מתודות קיימות
    Task<Shift?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shift shift);
    Task<bool> RegisterEmployeeForShiftAsync(Guid shiftId, string userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftRegistration>> GetPendingRegistrationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> ApproveShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default);

    
    Task<bool> RejectShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default);

   
    Task<IEnumerable<Employee>> GetApprovedEmployeesForShiftAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> GetApprovedEmployeesCountAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default);
    
}
