using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartShift.Domain.Features.Employees;
namespace SmartShift.Infrastructure.Features.Employees.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
   // Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<Employee> UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetAllAsync(Guid tenantId);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
} 