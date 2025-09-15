using MediatR;

namespace SmartShift.Application.Features.Scheduling.GetEmployeeShifts;

public sealed record GetEmployeeShiftsQuery(
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate
) : IRequest<GetEmployeeShiftsResult>;
