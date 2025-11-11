// SmartShift.Api/Features/Admin/Endpoints/CancelShiftModule.cs
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SmartShift.Application.Features.Scheduling.CancelShift;

namespace SmartShift.Api.Features.Admin.Endpoints;

public sealed class CancelShiftModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/admin/scheduling")
                       .RequireAuthorization()
                       .WithTags("Admin - Scheduling");

        group.MapDelete("/shifts/{shiftId:guid}", CancelShift)
             .WithSummary("Cancel a shift");
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> CancelShift(
        Guid shiftId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelShiftCommand(shiftId), cancellationToken);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }
}
