using MediatR;

namespace SmartShift.Application.Features.Scheduling.RegisterForShift;

public class RegisterForShiftCommand : IRequest<RegisterForShiftResult>
{
    public Guid ShiftId { get; set; }
    public Guid UserId { get; set; }
} 