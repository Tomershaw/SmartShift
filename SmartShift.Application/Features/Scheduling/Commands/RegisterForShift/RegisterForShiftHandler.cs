using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Features.Scheduling.Repositories;

namespace SmartShift.Application.Features.Scheduling.Commands.RegisterForShift
{
    public class RegisterForShiftHandler : IRequestHandler<RegisterForShiftCommand, RegisterForShiftResult>
    {
        private readonly IShiftRepository _shiftRepository;
        private readonly ICurrentUserService _currentUserService;

        public RegisterForShiftHandler(IShiftRepository shiftRepository, ICurrentUserService currentUserService)
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
}
