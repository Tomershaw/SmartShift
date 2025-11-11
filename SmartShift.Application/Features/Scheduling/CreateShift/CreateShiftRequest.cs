// SmartShift.Application/Features/Scheduling/CreateShift/CreateShiftRequest.cs
namespace SmartShift.Application.Features.Scheduling.CreateShift;

public sealed class CreateShiftRequest
{
    public required string Name { get; set; }
    public required DateTime StartTime { get; set; }
    public required int RequiredEmployeeCount { get; set; }
    public required int MinimumEmployeeCount { get; set; }
    public required int MinimumEarlyEmployees { get; set; }
    public required int SkillLevelRequired { get; set; }
    public required string Description { get; set; }
}
