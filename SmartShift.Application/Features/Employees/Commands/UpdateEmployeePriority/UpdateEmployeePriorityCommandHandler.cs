using MediatR;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Features.Employees.Repositories;

namespace SmartShift.Application.Features.Employees.Commands.UpdateEmployeePriority;

public class UpdateEmployeePriorityCommandHandler : IRequestHandler<UpdateEmployeePriorityCommand, EmployeeDto>
{
    private readonly IEmployeeRepository _employeeRepository;

    public UpdateEmployeePriorityCommandHandler(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<EmployeeDto> Handle(UpdateEmployeePriorityCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeRepository.GetByIdAsync(Guid.Parse(request.EmployeeId));
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {request.EmployeeId} not found");

        employee.UpdatePriorityRating(request.PriorityRating);
        await _employeeRepository.UpdateAsync(employee);

        return new EmployeeDto(
            employee.Id.ToString(),
            employee.Name,
            employee.PriorityRating
        );
    }
} 