namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesRequest
{
    public required List<Guid> EmployeeIds { get; set; } = new();
}