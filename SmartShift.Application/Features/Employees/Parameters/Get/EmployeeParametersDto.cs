namespace SmartShift.Application.Features.Employees.Parameters.Get;

public sealed record EmployeeParametersDto(
    int SkillLevel,
    int PriorityRating,
    int MaxShiftsPerWeek,
    string? AdminNotes
);
