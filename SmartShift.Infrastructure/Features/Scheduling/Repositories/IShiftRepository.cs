using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Infrastructure.Features.Scheduling.Repositories;

public interface IShiftRepository
{
    Task<Shift?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Shift>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shift shift);
}