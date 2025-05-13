using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.Queries.GetShifts;

namespace SmartShift.Api.Features.Scheduling.Endpoints;

public class ShiftsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling/shifts")
            .WithTags("Shifts")
            .WithOpenApi();

        group.MapGet("/", async ([FromQuery] string startDate, [FromQuery] string endDate, ISender sender) =>
        {
            Console.WriteLine($"📢 [API] GetShifts endpoint called! StartDate: {startDate}, EndDate: {endDate}");
            var result = await sender.Send(new GetShiftsQuery(startDate, endDate));
            return Results.Ok(result);
        })
        .WithName("GetShifts")
        .WithOpenApi();
    }
}