using MediatR;

namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesCommand : IRequest<ApproveShiftEmployeesResult>
{
    public required Guid ShiftId { get; set; }
    public required ApproveShiftEmployeesRequest Payload { get; set; }
}