using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SmartShift.Application.Features.Employees.Parameters.Get;

namespace SmartShift.Api.Features.Employees.Endpoints;

public sealed class EmployeeParametersGetEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin/employees")
                   .RequireAuthorization()
                   .WithTags("Admin - Employees");

        // GET /api/admin/employees/{employeeId}
        g.MapGet("/{employeeId:guid}", GetParameters)
         .WithSummary("Get current employee parameters");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> GetParameters(
        Guid employeeId,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new GetEmployeeParametersQuery(employeeId), ct);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }
}
