using MediatR;

namespace SmartShift.Application.Features.Employees.Commands.UpdateEmployeePriority;

public record UpdateEmployeePriorityCommand(string EmployeeId, int PriorityRating) : IRequest<EmployeeDto>;

public record EmployeeDto(
    string Id,
    string Name,
    int PriorityRating
); 