// SmartShift.Api/Features/Admin/Endpoints/CreateShiftModule.cs
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.CreateShift;

namespace SmartShift.Api.Features.Admin.Endpoints;

public sealed class CreateShiftModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/admin/scheduling")
                      .RequireAuthorization()
                      .WithTags("Admin - Scheduling");

        group.MapPost("/shifts", CreateShift)
             .WithSummary("Create a new shift");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> CreateShift(
        [FromBody] CreateShiftRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return Results.BadRequest("Request body is required");

        var command = new CreateShiftCommand { Payload = request };
        var result = await mediator.Send(command, cancellationToken);

        return result.Success
            ? Results.Ok(result)  // ✅ עקבי עם שאר הפרויקט
            : Results.BadRequest(result);
    }
}
