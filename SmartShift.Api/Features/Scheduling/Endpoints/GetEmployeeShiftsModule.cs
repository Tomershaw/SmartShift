using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.GetEmployeeShifts;

namespace SmartShift.Api.Features.Scheduling.Endpoints;

public class GetEmployeeShiftsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling/employees")
            .WithTags("Employee Shifts")
            .RequireAuthorization();

        group.MapGet("/my-shifts", async (
            HttpContext context,
            ISender sender,
            [FromQuery] string startDate,
            [FromQuery] string endDate) =>
        {
            // חילוץ employeeId מהטוקן
            var employeeIdClaim = context.User.FindFirst("employeeId")?.Value;
            if (!Guid.TryParse(employeeIdClaim, out var employeeId))
                return Results.BadRequest(new { error = "Employee ID not found or invalid in token" });

            if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd" });

            var result = await sender.Send(new GetEmployeeShiftsQuery(employeeId, start, end));

            if (!result.Success)
                return Results.BadRequest(new { error = result.Message });

            // בשלב 1 נשארים עם המחלקה הקיימת. לא משנים את הפורמט עדיין.
            return Results.Ok(result);
        })
        .WithName("GetMyShifts")
        .WithSummary("Get my shifts in date range")
        .WithDescription("Returns shift registrations for the current employee within a date range");
    }
}
