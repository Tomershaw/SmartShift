using MediatR;
using Microsoft.Extensions.Logging;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.Scheduling.RegisterForShift;
using SmartShift.Infrastructure.Repositories;
// ...

public class RegisterForShiftCommandHandler : IRequestHandler<RegisterForShiftCommand, RegisterForShiftResult>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RegisterForShiftCommandHandler> _logger;

    public RegisterForShiftCommandHandler(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService,
        ILogger<RegisterForShiftCommandHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<RegisterForShiftResult> Handle(RegisterForShiftCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // לוג קלטים
            _logger.LogInformation("RegisterForShift start: ShiftId={ShiftId}, ShiftArrivalType={Type}",
                request.ShiftId, request.ShiftArrivalType);

            // 1) ולידציה בסיסית על הבקשה
            if (request.ShiftId == Guid.Empty)
            {
                return new RegisterForShiftResult { Success = false, Message = "ShiftId חסר או לא תקין" };
            }

            // 2) שליפת מזהים מהטוקן
            var userIdStr = _currentUserService.GetUserId();
            var tenantIdObj = _currentUserService.GetTenantId(); // חלק מהמימושים מחזירים Guid, חלק string

            if (string.IsNullOrWhiteSpace(userIdStr))
            {
                _logger.LogWarning("Missing user id in token");
                return new RegisterForShiftResult { Success = false, Message = "לא נמצא מזהה משתמש בטוקן" };
            }

            // תרגום tenantId למבנה Guid בצורה בטוחה
            var tenantIdStr = tenantIdObj.ToString();
            if (!Guid.TryParse(tenantIdStr, out var tenantId))
            {
                _logger.LogWarning("Invalid tenant id in token: {TenantRaw}", tenantIdStr);
                return new RegisterForShiftResult { Success = false, Message = "TenantId בטוקן אינו תקין" };
            }

            // 3) קריאה לריפו עם טיפוסים מדויקים
            var ok = await _shiftRepository.RegisterEmployeeForShiftAsync(
                request.ShiftId,                 // Guid
                userIdStr,                       // string (UserId)
                tenantId,                        // Guid (TenantId)
                request.ShiftArrivalType,        // enum
                cancellationToken
            );

            return new RegisterForShiftResult
            {
                Success = ok,
                Message = ok
                    ? "בקשתך למשמרת נשלחה בהצלחה! ממתינה לאישור מנהל"
                    : "שליחת הבקשה נכשלה - ייתכן שכבר שלחת בקשה למשמרת זו"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in RegisterForShift");
            return new RegisterForShiftResult
            {
                Success = false,
                Message = "אירעה שגיאה פנימית בעת רישום למשמרת"
            };
        }
    }
}
