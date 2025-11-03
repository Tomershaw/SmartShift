using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Api.Requests;
using SmartShift.Application.Features.Employees.Parameters.Update;

namespace SmartShift.Api.Features.Employees.Endpoints;

public sealed class EmployeeParametersModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/employees") // ✅ מוביל עם slash
                   .RequireAuthorization()
                   .WithTags("Admin - Employees");

        g.MapPut("/{employeeId:guid}/parameters", UpdateParameters) // ✅ בלי רווחים
         .WithSummary("Update employee parameters (skill/priority/max/week/notes)");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> UpdateParameters(
        Guid employeeId,
        [FromBody] UpdateEmployeeParametersRequest body,
        IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new UpdateEmployeeParametersCommand(
            employeeId,
            body.SkillLevel,
            body.PriorityRating,
            body.MaxShiftsPerWeek,
            body.AdminNotes
        );

        var result = await mediator.Send(cmd, ct);
        return Results.Ok(result);
    }
}
