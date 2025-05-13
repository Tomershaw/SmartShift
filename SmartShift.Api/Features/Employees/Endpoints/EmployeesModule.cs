using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Employees.Commands.UpdateEmployeePriority;
using SmartShift.Application.Features.Employees.Queries.GetEmployees;

namespace SmartShift.Api.Features.Employees.Endpoints;

public class EmployeesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling/employees")
            .WithTags("Employees")
            .WithOpenApi();

        group.MapGet("/", async (ISender sender) =>
        {
            Console.WriteLine("📢 [API] GetEmployees endpoint was called!");
            var result = await sender.Send(new GetEmployeesQuery());
            return Results.Ok(result);
        })
        .WithName("GetEmployees")
        .WithOpenApi();

        group.MapPut("/{id}/priority", async (string id, [FromBody] UpdateEmployeePriorityRequest request, ISender sender) =>
        {
            Console.WriteLine($"📢 [API] UpdateEmployeePriority called for EmployeeId: {id} with Priority: {request.PriorityRating}");
            var command = new UpdateEmployeePriorityCommand(id, request.PriorityRating);
            var result = await sender.Send(command);
            return Results.Ok(result);
        })
        .WithName("UpdateEmployeePriority")
        .WithOpenApi();
    }
}

public record UpdateEmployeePriorityRequest(int PriorityRating);
