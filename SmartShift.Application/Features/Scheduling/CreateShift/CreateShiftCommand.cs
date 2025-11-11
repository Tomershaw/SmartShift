// SmartShift.Application/Features/Scheduling/CreateShift/CreateShiftCommand.cs
using MediatR;

namespace SmartShift.Application.Features.Scheduling.CreateShift;

public sealed class CreateShiftCommand : IRequest<CreateShiftResult>
{
    public required CreateShiftRequest Payload { get; set; }
}
