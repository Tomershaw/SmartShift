using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.Employees.Commands.UpdateEmployeePriority;
using SmartShift.Infrastructure.Features.Employees.Repositories;

public class UpdateEmployeePriorityCommandHandler : IRequestHandler<UpdateEmployeePriorityCommand, EmployeeDto>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateEmployeePriorityCommandHandler(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<EmployeeDto> Handle(UpdateEmployeePriorityCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.GetTenantId();

        var employee = await _employeeRepository.GetByIdAsync(
            Guid.Parse(request.EmployeeId),
            tenantId,
            cancellationToken
        );
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {request.EmployeeId} not found");

        if (employee.TenantId != tenantId)
            throw new UnauthorizedAccessException("You are not allowed to update this employee");

        employee.UpdatePriorityRating(request.PriorityRating);
        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return new EmployeeDto(
            employee.Id.ToString(),
            employee.Name,
            employee.PriorityRating
        );
    }
}
