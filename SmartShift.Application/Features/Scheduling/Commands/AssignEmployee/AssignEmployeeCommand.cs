using MediatR;

namespace SmartShift.Application.Features.Scheduling.Commands.AssignEmployee;

public record AssignEmployeeCommand(string ShiftId, string EmployeeId) : IRequest<ShiftDto>;

public record ShiftDto(
    string Id,
    DateTime StartTime,
    DateTime EndTime,
    string? AssignedEmployeeId
); 