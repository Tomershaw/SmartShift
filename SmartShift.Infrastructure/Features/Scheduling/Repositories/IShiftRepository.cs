using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Infrastructure.Features.Scheduling.Repositories;

public interface IShiftRepository
{
    Task<Shift?> GetByIdAsync(Guid id);
    Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate);
    Task UpdateAsync(Shift shift);
} 