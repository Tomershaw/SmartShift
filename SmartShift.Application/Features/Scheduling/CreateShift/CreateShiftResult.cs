// SmartShift.Application/Features/Scheduling/CreateShift/CreateShiftResult.cs
namespace SmartShift.Application.Features.Scheduling.CreateShift;

public sealed class CreateShiftResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime? StartTime { get; set; }
    public string? ShiftName { get; set; }
}
