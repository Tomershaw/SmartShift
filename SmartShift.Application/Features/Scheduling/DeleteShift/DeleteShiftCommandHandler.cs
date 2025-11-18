// SmartShift.Application/Features/Scheduling/DeleteShift/DeleteShiftCommandHandler.cs
using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.DeleteShift;

public sealed class DeleteShiftCommandHandler
    : IRequestHandler<DeleteShiftCommand, DeleteShiftResult>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteShiftCommandHandler(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteShiftResult> Handle(DeleteShiftCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _currentUserService.GetTenantId();
            var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, tenantId, cancellationToken);

            if (shift is null)
                return new DeleteShiftResult { Success = false, Message = "משמרת לא נמצאה" };

            // Business Rule: לא ניתן למחוק משמרת שכבר התחילה
            if (shift.StartTime <= DateTime.UtcNow)
            {
                return new DeleteShiftResult
                {
                    Success = false,
                    ShiftId = shift.Id,
                    StartTime = shift.StartTime,
                    ShiftName = shift.Name,
                    Message = "לא ניתן למחוק משמרת שכבר התחילה או הסתיימה"
                };
            }

            // Business Rule: בדוק אם יש הרשמות מאושרות
            //var approvedCount = await _shiftRepository.GetApprovedEmployeesCountAsync(shift.Id, tenantId, cancellationToken);
            //if (approvedCount > 0)
            //{
            //    return new DeleteShiftResult
            //    {
            //        Success = false,
            //        ShiftId = shift.Id,
            //        StartTime = shift.StartTime,
            //        ShiftName = shift.Name,
            //        Message = $"לא ניתן למחוק משמרת עם {approvedCount} הרשמות מאושרות. בטל תחילה את ההרשמות או השתמש בביטול במקום מחיקה."
            //    };
            //}

           // await _shiftRepository.DeleteAsync(shift.Id, tenantId, cancellationToken);
           await _shiftRepository.SoftDeleteAsync(shift.Id, tenantId, cancellationToken);

            return new DeleteShiftResult
            {
                Success = true,
                ShiftId = shift.Id,
                StartTime = shift.StartTime,
                ShiftName = shift.Name,
                Message = "המשמרת נמחקה בהצלחה"
            };
        }
        catch (Exception ex)
        {
            return new DeleteShiftResult { Success = false, Message = "שגיאה במחיקת המשמרת" };
        }
    }
}
