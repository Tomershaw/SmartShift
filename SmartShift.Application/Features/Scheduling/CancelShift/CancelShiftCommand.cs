// SmartShift.Application/Features/Scheduling/CancelShift/CancelShiftCommand.cs
using MediatR;

namespace SmartShift.Application.Features.Scheduling.CancelShift;

public sealed record CancelShiftCommand(Guid ShiftId) : IRequest<CancelShiftResult>;
