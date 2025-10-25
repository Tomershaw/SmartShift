using MediatR;

namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesCommand : IRequest<ApproveShiftEmployeesResult>
{
    public required ApproveShiftEmployeesRequest Payload { get; set; }
}