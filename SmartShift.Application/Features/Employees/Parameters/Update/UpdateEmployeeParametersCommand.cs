using MediatR;

namespace SmartShift.Application.Features.Employees.Parameters.Update;

public record UpdateEmployeeParametersCommand(
    Guid EmployeeId,
    int SkillLevel,
    int PriorityRating,
    int MaxShiftsPerWeek,
    string? AdminNotes
) : IRequest<UpdateEmployeeParametersResult>;
