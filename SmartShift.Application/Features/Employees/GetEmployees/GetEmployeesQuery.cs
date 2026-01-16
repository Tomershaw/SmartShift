using MediatR;

namespace SmartShift.Application.Features.Employees.GetEmployees;

public record GetEmployeesQuery : IRequest<IEnumerable<EmployeeDto>>;
 
public record EmployeeDto(
    string Id,
    string Name,
    int PriorityRating,
    bool IsActive,
    string? UserId
); 