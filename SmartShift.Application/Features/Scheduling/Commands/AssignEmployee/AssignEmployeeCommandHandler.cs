using MediatR;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Features.Employees.Repositories;
using SmartShift.Infrastructure.Features.Scheduling.Repositories;

namespace SmartShift.Application.Features.Scheduling.Commands.AssignEmployee;

public class AssignEmployeeCommandHandler : IRequestHandler<AssignEmployeeCommand, ShiftDto>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public AssignEmployeeCommandHandler(
        IShiftRepository shiftRepository,
        IEmployeeRepository employeeRepository)
    {
        _shiftRepository = shiftRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<ShiftDto> Handle(AssignEmployeeCommand request, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetByIdAsync(Guid.Parse(request.ShiftId));
        if (shift == null)
            throw new KeyNotFoundException($"Shift with ID {request.ShiftId} not found");

        var employee = await _employeeRepository.GetByIdAsync(Guid.Parse(request.EmployeeId));
        if (employee == null)
            throw new KeyNotFoundException($"Employee with ID {request.EmployeeId} not found");

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