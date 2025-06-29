using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Infrastructure.Features.Scheduling.Repositories
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly ApplicationDbContext _context;

        public ShiftRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Shift?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Shifts
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, cancellationToken);
        }

        public async Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Shifts
                .Where(s => s.TenantId == tenantId && s.StartTime >= startDate && s.EndTime <= endDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Shift>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Shifts
                .Where(s => s.TenantId == tenantId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default)
        {
            await _context.Shifts.AddAsync(shift, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return shift;
        }

        public async Task UpdateAsync(Shift shift)
        {
            _context.Shifts.Update(shift);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
        {
            var shift = await GetByIdAsync(id, tenantId, cancellationToken);
            if (shift != null)
            {
                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
