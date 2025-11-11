using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.DeleteShift;

namespace SmartShift.Api.Features.Admin.Endpoints;

public sealed class DeleteShiftModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/admin/scheduling")
            .RequireAuthorization()
            .WithTags("Admin - Scheduling");

        group.MapDelete("/shifts/{shiftId:guid}/hard-delete", DeleteShift)
            .WithName("DeleteShift")
            .WithSummary("Permanently delete a shift");
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> DeleteShift(
        [FromRoute] Guid shiftId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (shiftId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "shiftId is required" });
        }

        var result = await sender.Send(new DeleteShiftCommand(shiftId), cancellationToken);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }
}
