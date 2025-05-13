using MediatR;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Features.Employees.Repositories;

namespace SmartShift.Application.Features.Employees.Queries.GetEmployees;

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, IEnumerable<EmployeeDto>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public GetEmployeesQueryHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.GetAllAsync();
        
        return employees.Select(e => new EmployeeDto(
            e.Id.ToString(),
            e.Name,
            e.PriorityRating
        ));
    }
} 