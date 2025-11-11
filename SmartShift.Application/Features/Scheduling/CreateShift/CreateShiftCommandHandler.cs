// SmartShift.Application/Features/Scheduling/CreateShift/CreateShiftCommandHandler.cs
using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.CreateShift;

public sealed class CreateShiftCommandHandler
    : IRequestHandler<CreateShiftCommand, CreateShiftResult>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateShiftCommandHandler(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CreateShiftResult> Handle(CreateShiftCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payload = request.Payload;
            var tenantId = _currentUserService.GetTenantId();

            // בלימה עסקית: רק משמרת אחת ליום
            var exists = await _shiftRepository.ExistsShiftOnDateAsync( tenantId, payload.StartTime, cancellationToken);
            if (exists)
            {
                return new CreateShiftResult
                {
                    Success = false,
                    Message = "כבר קיימת משמרת בתאריך זה. לא ניתן ליצור יותר ממשמרת אחת לאותו יום."
                };
            }

            var shift = new Shift(
                payload.StartTime,
                payload.RequiredEmployeeCount,
                payload.MinimumEmployeeCount,
                payload.SkillLevelRequired,
                payload.Description,
                payload.MinimumEarlyEmployees
            )
            {
                Name = payload.Name,
                TenantId = tenantId
            };

            var createdShift = await _shiftRepository.AddAsync(shift, cancellationToken);

            return new CreateShiftResult
            {
                Success = true,
                ShiftId = createdShift.Id,
                StartTime = createdShift.StartTime,
                ShiftName = createdShift.Name,
                Message = "משמרת נוצרה בהצלחה"
            };
        }
        catch (Exception ex)
        {
            return new CreateShiftResult
            {
                Success = false,
                Message = "שגיאה ביצירת המשמרת"
            };
        }
    }
}
