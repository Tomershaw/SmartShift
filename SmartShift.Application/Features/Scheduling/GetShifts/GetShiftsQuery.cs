using MediatR;

namespace SmartShift.Application.Features.Scheduling.GetShifts;

public record GetShiftsQuery(string StartDate, string EndDate) : IRequest<IEnumerable<ShiftDto>>;

public record ShiftDto(
    string Id,
    DateTimeOffset StartTime,
    string? AssignedEmployeeId
); 



