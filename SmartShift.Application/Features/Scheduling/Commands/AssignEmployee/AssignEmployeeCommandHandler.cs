using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Infrastructure.Features.Employees.Repositories;
using SmartShift.Infrastructure.Features.Scheduling.Repositories;

namespace SmartShift.Application.Features.Scheduling.Commands.AssignEmployee;

public class AssignEmployeeCommandHandler : IRequestHandler<AssignEmployeeCommand, ShiftDto>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public AssignEmployeeCommandHandler(
        IShiftRepository shiftRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ShiftDto> Handle(AssignEmployeeCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.GetTenantId();

        var shift = await _shiftRepository.GetByIdAsync(Guid.Parse(request.ShiftId), tenantId);
        if (shift == null)
            throw new KeyNotFoundException($"Shift with ID {request.ShiftId} not found");

        if (shift.TenantId != tenantId)
            throw new UnauthorizedAccessException("You are not allowed to modify this shift");
        // אם בהמשך נרצה גם לבדוק shift.TenantId – זה המקום

        var employee = await _employeeRepository.GetByIdAsync(Guid.Parse(request.EmployeeId), tenantId, cancellationToken);
        if (employee == null)
            throw new UnauthorizedAccessException("Employee not found or does not belong to your tenant");

        shift.AssignEmployee(employee.Id);
        await _shiftRepository.UpdateAsync(shift);

        return new ShiftDto(
            shift.Id.ToString(),
            shift.StartTime,
            shift.EndTime,
            shift.AssignedEmployeeId?.ToString()
        );
    }
}
