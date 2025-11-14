using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.DayRegistrants;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Api.Features.Admin.Endpoints;

public sealed class DayRegistrantsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/registrations")
            .WithTags("Admin - Registrations")
            .RequireAuthorization(); // אם יש לך Policy Admin החלף ל-RequireAuthorization("Admin")

        // GET /api/admin/registrations/day?date=2025-11-20&status=Pending&skip=0&take=50
        group.MapGet("/day", GetDayRegistrants)
            .WithName("GetDayRegistrants")
            .WithSummary("Get registrant names for a local day")
            .WithDescription("Returns first/last names of registrants for a given local IL date, optionally filtered by status, with paging.");
    }
    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetDayRegistrants(
        [FromQuery(Name = "date")] string date,
        [FromQuery] string? status,
        [FromQuery] int? skip,
        [FromQuery] int? take,
        ISender sender)
    {
        if (!DateOnly.TryParse(date, out var dayLocal))
            return Results.BadRequest(new { error = "Invalid 'date'. Expected yyyy-MM-dd." });

        ShiftRegistrationStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ShiftRegistrationStatus>(status, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = "Invalid 'status'. Use Pending, Approved, Rejected, or Cancelled." });
            statusFilter = parsed;
        }

        var s = Math.Max(0, skip ?? 0);
        var t = Math.Max(1, Math.Min(take ?? 50, 200)); // ברירת מחדל 50, מקסימום 200

        var result = await sender.Send(new GetDayRegistrantsQuery(dayLocal, statusFilter, s, t));
        return Results.Ok(result);
    }
}
