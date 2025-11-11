// SmartShift.Application/Features/Scheduling/CancelShift/CancelShiftResult.cs
namespace SmartShift.Application.Features.Scheduling.CancelShift;

public sealed class CancelShiftResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime? StartTime { get; set; }
    public string? ShiftName { get; set; }
}
