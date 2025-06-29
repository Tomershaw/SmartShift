using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Infrastructure.Features.Employees.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees.ToListAsync(cancellationToken);
    }

    public async Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task<Employee> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var employee = await GetByIdAsync(id, tenantId, cancellationToken);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(Guid tenantId)
    {
        return await _context.Employees
            .Where(e => e.TenantId == tenantId)
            .ToListAsync();
    }

public async Task<Employee?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
{
    return await _context.Employees
        .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);
}
}