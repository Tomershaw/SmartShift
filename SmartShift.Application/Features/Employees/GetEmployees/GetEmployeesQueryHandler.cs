using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Employees.GetEmployees;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, IEnumerable<EmployeeDto>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;   
    }

    public async Task<IEnumerable<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.GetTenantId();
        var employees = await _employeeRepository.GetAllAsync(tenantId);

        return employees.Select(e => new EmployeeDto(
            e.Id.ToString(),
            e.Name,
            e.PriorityRating
        ));
    }
} 