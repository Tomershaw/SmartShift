using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.ShiftRegistrations;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Infrastructure.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ShiftRepository> _logger;

    public ShiftRepository(ApplicationDbContext context, ILogger<ShiftRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static (DateTime start, DateTime endExclusive) GetSunThuWindow(DateTime t)
    {
        var day = t.Date; // Sunday=0..Saturday=6
        int fromSunday = (int)day.DayOfWeek;
        var start = day.AddDays(-fromSunday); // יום ראשון של אותו שבוע
        var end = start.AddDays(5);         // עד חמישי - בלעדי
        return (start, end);
    }

    public async Task<WeekSnapshot> GetWeekAssignmentsSnapshotAsync(
        Guid tenantId,
        DateTime pivot,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetWeekAssignmentsSnapshotAsync called with empty tenant ID");
            throw new ArgumentException("TenantId cannot be empty", nameof(tenantId));
        }

        var (ws, we) = GetSunThuWindow(pivot);

        _logger.LogInformation("Building week snapshot for tenant {TenantId} window {Start}->{End}", tenantId, ws, we);

        // כמה נרשמו לשבוע הזה (Pending + Approved) לפי תאריך המשמרת בפועל
        var desired = await _context.ShiftRegistrations
            .Where(r => r.TenantId == tenantId
                        && r.Shift != null
                        && r.Shift.StartTime >= ws
                        && r.Shift.StartTime < we)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // כמה אושרו לשבוע הזה (Approved בלבד) לפי תאריך המשמרת בפועל
        var approvedWeek = await _context.ShiftRegistrations
            .Where(r => r.TenantId == tenantId
                        && r.Shift != null
                        && r.Shift.StartTime >= ws
                        && r.Shift.StartTime < we
                        && r.Status == ShiftRegistrationStatus.Approved)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // כמה אושרו בחודש של ה-pivot לפי תאריך המשמרת בפועל
        var monthStart = new DateTime(pivot.Year, pivot.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var approvedMonth = await _context.ShiftRegistrations
            .Where(r => r.TenantId == tenantId
                        && r.Shift != null
                        && r.Shift.StartTime >= monthStart
                        && r.Shift.StartTime < nextMonth
                        && r.Status == ShiftRegistrationStatus.Approved)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // מדיאן חודשי של הצוות
        var monthlyCounts = approvedMonth.Select(x => x.Count).OrderBy(x => x).ToList();
        var median = monthlyCounts.Count == 0 ? 0 : monthlyCounts[monthlyCounts.Count / 2];

        var snap = new WeekSnapshot { TeamMedianMonthly = median };

        foreach (var d in desired)
            snap.DesiredThisWeek[d.EmployeeId] = d.Count;

        foreach (var a in approvedWeek)
            snap.ApprovedThisWeek[a.EmployeeId] = a.Count;

        foreach (var m in approvedMonth)
            snap.ApprovedThisMonth[m.EmployeeId] = m.Count;

        _logger.LogInformation(
            "Week snapshot built. Desired={Desired}, ApprovedWeek={ApprovedWeek}, ApprovedMonth={ApprovedMonth}, MedianMonthly={Median}",
            snap.DesiredThisWeek.Count, snap.ApprovedThisWeek.Count, snap.ApprovedThisMonth.Count, snap.TeamMedianMonthly);

        return snap;
    }

    public async Task<Shift?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GetByIdAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetByIdAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Getting shift {ShiftId} for tenant {TenantId}", id, tenantId);

            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, cancellationToken);

            if (shift == null)
            {
                _logger.LogInformation("Shift {ShiftId} not found for tenant {TenantId}", id, tenantId);
            }

            return shift;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetByIdAsync operation was cancelled for shift {ShiftId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift {ShiftId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }



    public async Task<IEnumerable<Shift>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetAllAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Getting all shifts for tenant {TenantId}", tenantId);

            var shifts = await _context.Shifts
                .Where(s => s.TenantId == tenantId)
                .OrderBy(s => s.StartTime)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {ShiftCount} total shifts for tenant {TenantId}", shifts.Count, tenantId);
            return shifts;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetAllAsync operation was cancelled for tenant {TenantId}", tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all shifts for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (shift == null)
        {
            _logger.LogWarning("AddAsync called with null shift");
            throw new ArgumentNullException(nameof(shift));
        }

        if (shift.TenantId == null || shift.TenantId == Guid.Empty)
        {
            _logger.LogWarning("AddAsync called with invalid tenant ID");
            throw new ArgumentException("Shift must have a valid tenant ID", nameof(shift));
        }

        try
        {
            _logger.LogInformation("Adding new shift for tenant {TenantId}", shift.TenantId);

            await _context.Shifts.AddAsync(shift, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully added shift {ShiftId} for tenant {TenantId}", shift.Id, shift.TenantId);
            return shift;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AddAsync operation was cancelled for tenant {TenantId}", shift.TenantId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while adding shift for tenant {TenantId}", shift.TenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding shift for tenant {TenantId}", shift.TenantId);
            throw;
        }
    }

    public async Task UpdateAsync(Shift shift)
    {
        // Input validation
        if (shift == null)
        {
            _logger.LogWarning("UpdateAsync called with null shift");
            throw new ArgumentNullException(nameof(shift));
        }

        if (shift.Id == Guid.Empty)
        {
            _logger.LogWarning("UpdateAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(shift));
        }

        try
        {
            _logger.LogInformation("Updating shift {ShiftId} for tenant {TenantId}", shift.Id, shift.TenantId);

            _context.Shifts.Update(shift);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated shift {ShiftId}", shift.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while updating shift {ShiftId}", shift.Id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating shift {ShiftId}", shift.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift {ShiftId}", shift.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (id == Guid.Empty)
        {
            _logger.LogWarning("DeleteAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(id));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("DeleteAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Attempting to delete shift {ShiftId} for tenant {TenantId}", id, tenantId);

            var shift = await GetByIdAsync(id, tenantId, cancellationToken);
            if (shift != null)
            {
                _context.Shifts.Remove(shift);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully deleted shift {ShiftId}", id);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent shift {ShiftId} for tenant {TenantId}", id, tenantId);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("DeleteAsync operation was cancelled for shift {ShiftId}", id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting shift {ShiftId}", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift {ShiftId} for tenant {TenantId}", id, tenantId);
            throw;
        }
    }

    public async Task<bool> RegisterEmployeeForShiftAsync(Guid shiftId, string userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (shiftId == Guid.Empty)
        {
            _logger.LogWarning("RegisterEmployeeForShiftAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(shiftId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("RegisterEmployeeForShiftAsync called with invalid user ID");
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("RegisterEmployeeForShiftAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Registering user {UserId} for shift {ShiftId} in tenant {TenantId}", userId, shiftId, tenantId);

            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.Id == shiftId && s.TenantId == tenantId, cancellationToken);

            if (shift == null)
            {
                _logger.LogWarning("Shift {ShiftId} not found for tenant {TenantId}", shiftId, tenantId);
                return false;
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);

            if (employee == null)
            {
                _logger.LogWarning("Employee with user ID {UserId} not found in tenant {TenantId}", userId, tenantId);
                return false;
            }

            var tenant = await _context.Tenants.FindAsync(new object[] { tenantId }, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found", tenantId);
                return false;
            }

            var existingRequest = await _context.ShiftRegistrations
                .AnyAsync(sr => sr.ShiftId == shiftId &&
                               sr.EmployeeId == employee.Id &&
                               sr.TenantId == tenantId &&
                               (sr.Status == ShiftRegistrationStatus.Pending ||
                                sr.Status == ShiftRegistrationStatus.Approved),
                          cancellationToken);

            if (existingRequest)
            {
                _logger.LogWarning("Employee {EmployeeId} already has a registration for shift {ShiftId}", employee.Id, shiftId);
                return false;
            }

            var registration = new ShiftRegistration(shiftId, employee.Id, tenantId, EmployeeShiftAvailability.Regular)
            {
                Employee = employee,
                Shift = shift,
                Tenant = tenant
            }; ;

            await _context.ShiftRegistrations.AddAsync(registration, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully registered employee {EmployeeId} for shift {ShiftId}", employee.Id, shiftId);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RegisterEmployeeForShiftAsync operation was cancelled for shift {ShiftId}", shiftId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while registering employee for shift {ShiftId}", shiftId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {UserId} for shift {ShiftId} in tenant {TenantId}", userId, shiftId, tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<ShiftRegistration>> GetPendingRegistrationsAsync(
     Guid tenantId,
     Guid shiftId,
     CancellationToken cancellationToken = default)
    {
        // Guard clauses
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetPendingRegistrationsAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }
        if (shiftId == Guid.Empty)
        {
            _logger.LogWarning("GetPendingRegistrationsAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(shiftId));
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TenantId"] = tenantId,
            ["ShiftId"] = shiftId
        });

        try
        {
            _logger.LogInformation("Fetching pending registrations for shift");

            var registrations = await _context.ShiftRegistrations
                .AsNoTracking()
                .Include(sr => sr.Employee) // דרוש ל-AI
                .Where(sr => sr.TenantId == tenantId
                          && sr.Status == ShiftRegistrationStatus.Pending
                          && sr.ShiftId == shiftId)
                .OrderBy(sr => sr.RegisteredAt)
                .ThenBy(sr => sr.Id) // סדר דטרמיניסטי
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Fetched {RegistrationCount} pending registrations for shift {ShiftId}, tenant {TenantId}",
                registrations.Count, shiftId, tenantId);

            return registrations;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetPendingRegistrationsAsync was cancelled for shift {ShiftId}, tenant {TenantId}", shiftId, tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending registrations for shift {ShiftId}, tenant {TenantId}", shiftId, tenantId);
            throw;
        }
    }


    //public async Task<IEnumerable<ShiftRegistration>> GetApprovedRegistrationsAsync(
    //Guid tenantId,
    //IEnumerable<Guid> shiftIds,
    //CancellationToken cancellationToken = default)
    //{
    //    if (tenantId == Guid.Empty)
    //        throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
    //    if (shiftIds is null)
    //        throw new ArgumentNullException(nameof(shiftIds));

    //    var ids = shiftIds.Distinct().ToList();
    //    if (ids.Count == 0)
    //        return Enumerable.Empty<ShiftRegistration>();

    //    try
    //    {
    //        _logger.LogInformation("Getting approved registrations for {Count} shifts, tenant {TenantId}",
    //            ids.Count, tenantId);

    //        var registrations = await _context.ShiftRegistrations
    //            .Include(sr => sr.Shift)
    //            .Include(sr => sr.Employee)
    //            .Where(sr => sr.TenantId == tenantId &&
    //                         sr.Status == ShiftRegistrationStatus.Approved &&
    //                         ids.Contains(sr.ShiftId))
    //            .OrderBy(sr => sr.ReviewedAt ?? sr.RegisteredAt)
    //            .ToListAsync(cancellationToken);

    //        _logger.LogInformation("Found {Count} approved registrations for tenant {TenantId}",
    //            registrations.Count, tenantId);

    //        return registrations;
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        _logger.LogWarning("GetApprovedRegistrationsAsync (by shifts) was cancelled for tenant {TenantId}", tenantId);
    //        throw;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error getting approved registrations (by shifts) for tenant {TenantId}", tenantId);
    //        throw;
    //    }
    //}

    public async Task<bool> ApproveShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (registrationId == Guid.Empty)
        {
            _logger.LogWarning("ApproveShiftRegistrationAsync called with empty registration ID");
            throw new ArgumentException("Registration ID cannot be empty", nameof(registrationId));
        }

        if (reviewedBy == Guid.Empty)
        {
            _logger.LogWarning("ApproveShiftRegistrationAsync called with empty reviewer ID");
            throw new ArgumentException("Reviewer ID cannot be empty", nameof(reviewedBy));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("ApproveShiftRegistrationAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Approving registration {RegistrationId} by reviewer {ReviewedBy} in tenant {TenantId}",
                registrationId, reviewedBy, tenantId);

            var registration = await _context.ShiftRegistrations
                .FirstOrDefaultAsync(sr => sr.Id == registrationId && sr.TenantId == tenantId, cancellationToken);

            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found in tenant {TenantId}", registrationId, tenantId);
                return false;
            }

            if (registration.Status != ShiftRegistrationStatus.Pending)
            {
                _logger.LogWarning("Registration {RegistrationId} is not in pending status (current: {Status})",
                    registrationId, registration.Status);
                return false;
            }

            registration.Approve(reviewedBy, comment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully approved registration {RegistrationId}", registrationId);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ApproveShiftRegistrationAsync operation was cancelled for registration {RegistrationId}", registrationId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while approving registration {RegistrationId}", registrationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving registration {RegistrationId} in tenant {TenantId}", registrationId, tenantId);
            throw;
        }
    }

    public async Task<bool> RejectShiftRegistrationAsync(Guid registrationId, Guid reviewedBy, string? comment, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (registrationId == Guid.Empty)
        {
            _logger.LogWarning("RejectShiftRegistrationAsync called with empty registration ID");
            throw new ArgumentException("Registration ID cannot be empty", nameof(registrationId));
        }

        if (reviewedBy == Guid.Empty)
        {
            _logger.LogWarning("RejectShiftRegistrationAsync called with empty reviewer ID");
            throw new ArgumentException("Reviewer ID cannot be empty", nameof(reviewedBy));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("RejectShiftRegistrationAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Rejecting registration {RegistrationId} by reviewer {ReviewedBy} in tenant {TenantId}",
                registrationId, reviewedBy, tenantId);

            var registration = await _context.ShiftRegistrations
                .FirstOrDefaultAsync(sr => sr.Id == registrationId && sr.TenantId == tenantId, cancellationToken);

            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found in tenant {TenantId}", registrationId, tenantId);
                return false;
            }

            if (registration.Status != ShiftRegistrationStatus.Pending)
            {
                _logger.LogWarning("Registration {RegistrationId} is not in pending status (current: {Status})",
                    registrationId, registration.Status);
                return false;
            }

            registration.Reject(reviewedBy, comment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully rejected registration {RegistrationId}", registrationId);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RejectShiftRegistrationAsync operation was cancelled for registration {RegistrationId}", registrationId);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while rejecting registration {RegistrationId}", registrationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting registration {RegistrationId} in tenant {TenantId}", registrationId, tenantId);
            throw;
        }
    }

    public async Task<IEnumerable<Employee>> GetApprovedEmployeesForShiftAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (shiftId == Guid.Empty)
        {
            _logger.LogWarning("GetApprovedEmployeesForShiftAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(shiftId));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetApprovedEmployeesForShiftAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Getting approved employees for shift {ShiftId} in tenant {TenantId}", shiftId, tenantId);

            var employees = await _context.ShiftRegistrations
                .Where(sr => sr.ShiftId == shiftId &&
                            sr.TenantId == tenantId &&
                            sr.Status == ShiftRegistrationStatus.Approved)
                .Include(sr => sr.Employee)
                .Select(sr => sr.Employee!)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {EmployeeCount} approved employees for shift {ShiftId}", employees.Count, shiftId);
            return employees;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetApprovedEmployeesForShiftAsync operation was cancelled for shift {ShiftId}", shiftId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approved employees for shift {ShiftId} in tenant {TenantId}", shiftId, tenantId);
            throw;
        }
    }

    public async Task<int> GetApprovedEmployeesCountAsync(Guid shiftId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (shiftId == Guid.Empty)
        {
            _logger.LogWarning("GetApprovedEmployeesCountAsync called with empty shift ID");
            throw new ArgumentException("Shift ID cannot be empty", nameof(shiftId));
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetApprovedEmployeesCountAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Getting approved employee count for shift {ShiftId} in tenant {TenantId}", shiftId, tenantId);

            var count = await _context.ShiftRegistrations
                .CountAsync(sr => sr.ShiftId == shiftId &&
                                 sr.TenantId == tenantId &&
                                 sr.Status == ShiftRegistrationStatus.Approved, cancellationToken);

            _logger.LogInformation("Found {EmployeeCount} approved employees for shift {ShiftId}", count, shiftId);
            return count;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetApprovedEmployeesCountAsync operation was cancelled for shift {ShiftId}", shiftId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting approved employee count for shift {ShiftId} in tenant {TenantId}", shiftId, tenantId);
            throw;
        }
    }

    // הוסף את המתודה החסרה שזוהתה קודם
    public async Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Input validation
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetShiftsInDateRangeAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        if (startDate == default)
        {
            _logger.LogWarning("GetShiftsInDateRangeAsync called with default start date");
            throw new ArgumentException("Start date cannot be default", nameof(startDate));
        }

        if (endDate == default)
        {
            _logger.LogWarning("GetShiftsInDateRangeAsync called with default end date");
            throw new ArgumentException("End date cannot be default", nameof(endDate));
        }

        if (startDate > endDate)
        {
            _logger.LogWarning("GetShiftsInDateRangeAsync called with start date after end date");
            throw new ArgumentException("Start date cannot be after end date", nameof(startDate));
        }

        try
        {
            _logger.LogInformation("Getting shifts from {StartDate} to {EndDate} for tenant {TenantId}", startDate, endDate, tenantId);

            var shifts = await _context.Shifts
                .Where(s => s.TenantId == tenantId &&
                           s.StartTime >= startDate &&
                           s.StartTime <= endDate)
                .Include(s => s.ShiftRegistrations)
                    .ThenInclude(sr => sr.Employee)
                .OrderBy(s => s.StartTime)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {ShiftCount} shifts for tenant {TenantId} in date range", shifts.Count, tenantId);
            return shifts;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetShiftsInDateRangeAsync operation was cancelled for tenant {TenantId}", tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shifts for tenant {TenantId} from {StartDate} to {EndDate}", tenantId, startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// שליפת כל העובדים עבור Tenant ספציפי
    /// </summary>
    public async Task<IEnumerable<Employee>> GetAllEmployeesForTenantAsync(
     Guid tenantId,
     CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("GetAllEmployeesForTenantAsync called with empty tenant ID");
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        try
        {
            _logger.LogInformation("Getting all employees for tenant {TenantId}", tenantId);

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(e => e.TenantId == tenantId)
                .OrderBy(e => e.FirstName ?? string.Empty) 
                .ThenBy(e => e.LastName ?? string.Empty)  
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {EmployeeCount} employees for tenant {TenantId}",
                employees.Count, tenantId);

            return employees;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetAllEmployeesForTenantAsync operation was cancelled for tenant {TenantId}", tenantId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all employees for tenant {TenantId}", tenantId);
            throw;
        }
    }

}


