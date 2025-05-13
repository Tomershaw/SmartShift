using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Infrastructure.Features.Scheduling.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly ApplicationDbContext _context;

    public ShiftRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Shift?> GetByIdAsync(Guid id)
    {
        return await _context.Shifts.FindAsync(id);
    }

    public async Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Shifts
            .Where(s => s.StartTime >= startDate && s.EndTime <= endDate)
            .ToListAsync();
    }

    public async Task UpdateAsync(Shift shift)
    {
        _context.Shifts.Update(shift);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Shift>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Shifts.ToListAsync(cancellationToken);
    }

    public async Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        await _context.Shifts.AddAsync(shift, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return shift;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shift = await GetByIdAsync(id);
        if (shift != null)
        {
            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
} 