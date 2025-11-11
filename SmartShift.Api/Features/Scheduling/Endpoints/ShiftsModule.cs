using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.GetShifts;

namespace SmartShift.Api.Features.Scheduling.Endpoints;

public sealed class GetShiftsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling/shifts")
            .WithTags("Scheduling - Shifts")
            .RequireAuthorization();

        group.MapGet("", GetShifts)
            .WithName("GetShifts")
            .WithSummary("Get all shifts in date range")
            .WithDescription("Returns all shifts within the specified date range for the current tenant");
    }

    private static async Task<IResult> GetShifts(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        ISender sender)
    {
        if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
        {
            return Results.BadRequest(new { error = "startDate and endDate query parameters are required. Format: yyyy-MM-dd" });
        }

        var query = new GetShiftsQuery(startDate, endDate);
        var result = await sender.Send(query);

        return Results.Ok(result);
    }
}

