using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.ShiftRegistrations; // 🔧 Fix this line
using static SmartShift.Domain.Features.ShiftRegistrations.ShiftRegistration;

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

        public async Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate,Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.Shifts
                .Where(s => s.TenantId == tenantId && s.StartTime >= startDate )
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

        // עדכון - עכשיו זה יוצר בקשה במקום רישום ישיר
        public async Task<bool> RegisterEmployeeForShiftAsync(Guid shiftId, string userId, Guid tenantId, CancellationToken cancellationToken = default)
        {
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.Id == shiftId && s.TenantId == tenantId, cancellationToken);

            if (shift == null)
                return false;

            var employee = await _context.Employees.FirstOrDefaultAsync(x => x.UserId == userId);
            if (employee == null)
                return false;
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null)
                return false;

            //var employeeExists = await _context.Employees
            //    .AnyAsync(e => e.Id == employeeId && e.TenantId == tenantId, cancellationToken);

            //if (!employeeExists)
            //    return false;

            var existingRequest = await _context.ShiftRegistrations
                .Include(sr => sr.Employee)  // לוודא שיש נתונים על Employee
                .AnyAsync(sr => sr.ShiftId == shiftId && sr.EmployeeId == employee.Id &&
                               (sr.Status == ShiftRegistrationStatus.Pending || sr.Status == ShiftRegistrationStatus.Approved),
                          cancellationToken);

            if (existingRequest)
                return false;

          //  if (shift.StartTime <= DateTime.UtcNow)
          //      return false;


            var registration = new ShiftRegistration(shiftId, employee.Id, tenantId)
            {
                Employee = employee,
                Shift = shift,
                Tenant = tenant
            };

            await _context.ShiftRegistrations.AddAsync(registration, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }


        // מתודות חדשות לניהול בקשות
        public async Task<IEnumerable<ShiftRegistration>> GetPendingRegistrationsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.ShiftRegistrations
                .Include(sr => sr.Shift)
                .Include(sr => sr.Employee)
                .Where(sr => sr.TenantId == tenantId && sr.Status == ShiftRegistrationStatus.Pending)
                .OrderBy(sr => sr.RegisteredAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ApproveShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default)
        {
            var registration = await _context.ShiftRegistrations
                .FirstOrDefaultAsync(sr => sr.Id == registrationId && sr.TenantId == tenantId, cancellationToken);

            if (registration == null || registration.Status != ShiftRegistrationStatus.Pending)
                return false;

            registration.Approve(reviewedBy, comment);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        // 🟢 Add the missing method:
        public async Task<bool> RejectShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default)
        {
            var registration = await _context.ShiftRegistrations
                .FirstOrDefaultAsync(sr => sr.Id == registrationId && sr.TenantId == tenantId, cancellationToken);

            if (registration == null || registration.Status != ShiftRegistrationStatus.Pending)
                return false;

            registration.Reject(reviewedBy, comment);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<Employee>> GetApprovedEmployeesForShiftAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.ShiftRegistrations
                .Where(sr => sr.ShiftId == shiftId &&
                             sr.TenantId == tenantId &&
                             sr.Status == ShiftRegistrationStatus.Approved)
                .Include(sr => sr.Employee)
                .Select(sr => sr.Employee!)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetApprovedEmployeesCountAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default)
        {
            return await _context.ShiftRegistrations
                .CountAsync(sr => sr.ShiftId == shiftId &&
                                 sr.TenantId == tenantId &&
                                 sr.Status == ShiftRegistrationStatus.Approved, cancellationToken);
        }

    }
}
