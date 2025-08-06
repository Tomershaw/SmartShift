using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.RegisterForShift;

public class RegisterForShiftCommandHandler : IRequestHandler<RegisterForShiftCommand, RegisterForShiftResult>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;

    public RegisterForShiftCommandHandler(IShiftRepository shiftRepository, ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RegisterForShiftResult> Handle(RegisterForShiftCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        var tenantId = _currentUserService.GetTenantId();   
        var currentTenantId = tenantId;

        var result = await _shiftRepository.RegisterEmployeeForShiftAsync(
            request.ShiftId,    // Guid
            userId,         // Guid ✅
            tenantId,           // Guid
            cancellationToken   // CancellationToken
        );

        return new RegisterForShiftResult
        {
            Success = result,
            Message = result
                ? "בקשתך למשמרת נשלחה בהצלחה! ממתינה לאישור מנהל"
                : "שליחת הבקשה נכשלה - ייתכן שכבר שלחת בקשה למשמרת זו"
        };
    }
} 