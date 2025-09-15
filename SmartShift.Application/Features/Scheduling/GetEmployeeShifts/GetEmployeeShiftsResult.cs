namespace SmartShift.Application.Features.Scheduling.GetEmployeeShifts;

public class GetEmployeeShiftsResult
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public IEnumerable<EmployeeShiftDto> Shifts { get; set; } = [];
}

public record EmployeeShiftDto
{
    public string ShiftId { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public string? ShiftName { get; init; }
    public string? Description { get; init; }
    public int RequiredEmployeeCount { get; init; }
    public int MinimumEmployeeCount { get; init; }
    public int SkillLevelRequired { get; init; }
    public string RegistrationId { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public string RegistrationStatus { get; init; } = string.Empty;
    public string ShiftArrivalType { get; init; } = string.Empty;
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewComment { get; init; }
}
