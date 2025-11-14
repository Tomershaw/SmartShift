using MediatR;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Application.Features.Scheduling.DayRegistrants;

public sealed record GetDayRegistrantsQuery(
    DateOnly DayLocal,
    ShiftRegistrationStatus? Status = null,
    int Skip = 0,
    int Take = 100,
    EmployeeShiftAvailability? ArrivalType = null  // הוספת פרמטר חדש
) : IRequest<IReadOnlyList<DayRegistrantNameDto>>;
