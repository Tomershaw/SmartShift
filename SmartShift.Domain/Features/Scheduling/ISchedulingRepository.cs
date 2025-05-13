using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartShift.Domain.Features.Employees;

namespace SmartShift.Domain.Features.Scheduling;

public interface ISchedulingRepository
{
    Task<Employee?> GetEmployeeByIdAsync(Guid id);
    Task<IEnumerable<Employee>> GetAvailableEmployeesAsync(DateTime startTime, DateTime endTime);
    Task<Shift?> GetShiftByIdAsync(Guid id);
    Task<IEnumerable<Shift>> GetShiftsInDateRangeAsync(DateTime startDate, DateTime endDate);
    Task UpdateShiftAsync(Shift shift);
} 