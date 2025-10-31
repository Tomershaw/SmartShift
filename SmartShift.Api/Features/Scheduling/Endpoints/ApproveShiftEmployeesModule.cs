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
        if (body.EmployeeIds is null || body.EmployeeIds.Count == 0)
            return Results.BadRequest("EmployeeIds required");

        var cmd = new ApproveShiftEmployeesCommand
        {
            ShiftId = shiftId,
            Payload = body
        };

        var res = await mediator.Send(cmd, ct);
        return res.Success ? Results.Ok(res)
                           : Results.Problem(title: "Approve failed", detail: res.Message, statusCode: 400);
    }

}
