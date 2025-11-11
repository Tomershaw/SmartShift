// SmartShift.Application/Features/Scheduling/CancelShift/CancelShiftCommandHandler.cs
using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Application.Features.Scheduling.CancelShift;

public sealed class CancelShiftCommandHandler
    : IRequestHandler<CancelShiftCommand, CancelShiftResult>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;

    public CancelShiftCommandHandler(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CancelShiftResult> Handle(CancelShiftCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.GetTenantId();

        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, tenantId, cancellationToken);
        if (shift is null)
        {
            return new CancelShiftResult
            {
                Success = false,
                Message = "משמרת לא נמצאה"
            };
        }

        if (shift.StartTime <= DateTime.UtcNow)
        {
            return new CancelShiftResult
            {
                Success = false,
                ShiftId = shift.Id,
                StartTime = shift.StartTime,
                ShiftName = shift.Name,
                Message = "לא ניתן לבטל משמרת שכבר התחילה או הסתיימה"
            };
        }

        shift.Cancel(); 
        await _shiftRepository.UpdateAsync(shift);

        return new CancelShiftResult
        {
            Success = true,
            ShiftId = shift.Id,
            StartTime = shift.StartTime,
            ShiftName = shift.Name,
            Message = "המשמרת בוטלה בהצלחה"
        };
    }
}
