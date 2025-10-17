using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Common.Interfaces; // כמו ב-ProcessShiftsModule
using SmartShift.Application.Features.Scheduling.CancelRegistration;

namespace SmartShift.Api.Features.Scheduling.Endpoints;

public sealed class CancelRegistrationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/scheduling")
                       .WithTags("Scheduling")
                       .RequireAuthorization();

        group.MapPost("/shifts/{shiftId:guid}/cancel", CancelMyRegistration)
             .WithName("CancelShiftRegistration")
             .WithSummary("Cancel the current user's registration for a shift - allowed only when Pending.");
    }

    private static async Task<IResult> CancelMyRegistration(
        [FromRoute] Guid shiftId,
        ICurrentUserService currentUser,   // כמו ב-ProcessShifts
        IMediator mediator,
        CancellationToken ct)
    {
        var tenantId = currentUser.GetTenantId();
        var userId = currentUser.GetUserId(); 

        var result = await mediator.Send(new CancelRegistrationCommand(shiftId, userId, tenantId), ct);

        return result.Success
            ? Results.Ok(new { success = true })
            : Results.Conflict(new { success = false, error = result.Error.ToString(), message = "Pending only or not found" });
    }
}
