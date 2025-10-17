using MediatR;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.CancelRegistration
{
    public sealed class CancelRegistrationCommandHandler(IShiftRepository repo) : IRequestHandler<CancelRegistrationCommand, CancelRegistrationResult>
    {
        public async Task<CancelRegistrationResult> Handle(CancelRegistrationCommand request, CancellationToken ct)
        {
            var ok = await repo.CancelRegistrationAsync(request.ShiftId, request.UserId, request.TenantId, ct);
            return ok
                ? new CancelRegistrationResult(true)
                : new CancelRegistrationResult(false, CancelRegistrationError.NotFoundOrNotPending);
        }
    }
}
