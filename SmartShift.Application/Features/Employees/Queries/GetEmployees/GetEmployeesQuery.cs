using MediatR;

namespace SmartShift.Application.Features.Employees.Queries.GetEmployees;

public record GetEmployeesQuery : IRequest<IEnumerable<EmployeeDto>>;
 
public record EmployeeDto(
    string Id,
    string Name,
    int PriorityRating
); 