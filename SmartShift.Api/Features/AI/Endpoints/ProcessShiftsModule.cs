// SmartShift.Api/Features/AI/Endpoints/ProcessShiftsModule.cs
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.ProcessShifts;

namespace SmartShift.Api.Features.AI.Endpoints;

public class ProcessShiftsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/ai/shifts")
            .WithTags("AI - Shifts")
            .RequireAuthorization();

        group.MapGet("/process", ProcessShiftsForDateRange)
             .WithName("ProcessShiftsForDateRange")
             .WithSummary("Process shifts for a given date range. If dates are omitted, handler will auto-calc next week (Sun-Thu).");
    }
        
    private static async Task<IResult> ProcessShiftsForDateRange(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        ICurrentUserService currentUserService,
        IMediator mediator,
        ILogger<ProcessShiftsModule> logger,
        CancellationToken ct)
    {
        var tenantId = currentUserService.GetTenantId();

        var cmd = new ProcessShiftsCommand   
        {
            TenantId = tenantId,
            // מעבירים מחרוזות “גלם” – ה-Handler יפרש/יחליט על ברירת מחדל
            StartString = startDate,
            EndString   = endDate
        };

        var result = await mediator.Send(cmd, ct);

        if (result.Results.Count == 0)
            return Results.Ok(new { message = result.Message ?? "No shifts found in the specified date range." });

        return Results.Ok(result.Results);
    }
}