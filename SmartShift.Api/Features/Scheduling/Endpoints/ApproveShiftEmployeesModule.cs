using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

namespace SmartShift.Api.Features.Scheduling.Endpoints;

public sealed class ApproveShiftEmployeesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("api/admin/scheduling")
                   .RequireAuthorization()
                   .WithTags("Admin - Scheduling");

        g.MapPost("/shifts/{shiftId:guid}/approve-employees", ApproveEmployees)
         .WithSummary("Approve pending registrations for a shift");
    }

    private static async Task<IResult> ApproveEmployees(
        Guid shiftId,
        [FromBody] ApproveShiftEmployeesRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        if (body == null) return Results.BadRequest("Body required");
        if (body.ShiftId == Guid.Empty) body.ShiftId = shiftId;
        if (body.ShiftId != shiftId) return Results.BadRequest("ShiftId mismatch");

        var res = await mediator.Send(new ApproveShiftEmployeesCommand { Payload = body }, ct);
        return Results.Ok(res);
    }
}
