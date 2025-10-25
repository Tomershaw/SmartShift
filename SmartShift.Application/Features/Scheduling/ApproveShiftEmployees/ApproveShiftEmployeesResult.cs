namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesResult
{
    public bool Success { get; set; }
    public int ApprovedCount { get; set; }
    public string? Message { get; set; }

    // חדש - למעקב ואבחון
    public List<Guid> ApprovedIds { get; set; } = new();
    public List<Guid> RequestedButNotPending { get; set; } = new();
    public List<Guid> StillPendingAfterApprove { get; set; } = new(); // המבוקשים שנותרו Pending
}
