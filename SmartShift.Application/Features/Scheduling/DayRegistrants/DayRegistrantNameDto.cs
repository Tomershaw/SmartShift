using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Application.Features.Scheduling.DayRegistrants;

public sealed record DayRegistrantNameDto(
    string FirstName,
    string LastName,
    ShiftRegistrationStatus Status,
    EmployeeShiftAvailability ShiftArrivalType
);
