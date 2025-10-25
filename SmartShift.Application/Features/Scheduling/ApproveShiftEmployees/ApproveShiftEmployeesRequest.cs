namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesRequest
{
    public required Guid ShiftId { get; set; }
    public required List<Guid> EmployeeIds { get; set; } = new();
}