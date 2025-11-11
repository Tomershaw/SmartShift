// SmartShift.Application/Features/Scheduling/DeleteShift/DeleteShiftResult.cs
namespace SmartShift.Application.Features.Scheduling.DeleteShift;

public sealed class DeleteShiftResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime? StartTime { get; set; }
    public string? ShiftName { get; set; }
}
