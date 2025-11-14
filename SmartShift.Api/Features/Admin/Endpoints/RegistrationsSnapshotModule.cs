using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.RegistrationsSnapshot;

namespace SmartShift.Api.Features.Admin.Endpoints;

public sealed class RegistrationsSnapshotModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/registrations")
            .WithTags("Admin - Registrations")
            .RequireAuthorization(); // אם יש לך Policy Admin, החלף ל-RequireAuthorization("Admin")

        // GET /api/admin/registrations/snapshot?from=2025-11-16&to=2025-11-20
        group.MapGet("/snapshot", GetSnapshot)
            .WithName("GetRegistrationsSnapshot")
            .WithSummary("Get daily registration snapshot")
            .WithDescription("Returns per-day counts for required and registered employees, broken down by status, for local IL dates.");
    }
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetSnapshot(
        [FromQuery] string from,
        [FromQuery] string to,
        ISender sender)
    {
        if (!DateOnly.TryParse(from, out var fromLocal) || !DateOnly.TryParse(to, out var toLocal))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd for 'from' and 'to'." });
        }

        if (toLocal < fromLocal)
        {
            return Results.BadRequest(new { error = "'to' must be on or after 'from'." });
        }

        var data = await sender.Send(new GetRegistrationsSnapshotQuery(fromLocal, toLocal));
        return Results.Ok(data);
    }
}
