using MediatR;

namespace SmartShift.Application.Features.Scheduling.CancelRegistration
{
    public sealed class CancelRegistrationCommand : IRequest<CancelRegistrationResult>
    {
        public Guid ShiftId { get; set; }
        public string UserId { get; set; } = default!;
        public Guid TenantId { get; set; }

        public CancelRegistrationCommand() { }

        public CancelRegistrationCommand(Guid shiftId, string userId, Guid tenantId)
        {
            ShiftId = shiftId;
            UserId = userId;
            TenantId = tenantId;
        }
    }
}
