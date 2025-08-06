using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SmartShift.Application.Features.Employees.UpdateEmployeePriority;
using SmartShift.Application.Features.Employees.GetEmployees;

namespace SmartShift.Api.Features.Employees.Endpoints;

public class EmployeesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling/employees")
            .WithTags("Employees")
            .WithOpenApi();

        group.MapGet("/", [Authorize(Roles = "Admin")] async (HttpContext httpContext, ISender sender, ILogger<EmployeesModule> logger) =>
        {
            var user = httpContext.User;

            logger.LogInformation("📢 [API] GetEmployees endpoint was called!");
            logger.LogInformation("📢 IsAuthenticated: {Auth}", user.Identity?.IsAuthenticated);
            logger.LogInformation("📢 User Name: {Name}", user.Identity?.Name);

            // ✅ טוקן גולמי מה-Header
            var token = httpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            logger.LogInformation("📢 Raw Token: {Token}", token);

            // ✅ הדפסת כל ה-Claims
            foreach (var claim in user.Claims)
            {
                logger.LogInformation("📢 Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
            }

            var result = await sender.Send(new GetEmployeesQuery());
            return Results.Ok(result);
        })
        .WithName("GetEmployees")
        .WithOpenApi();

        group.MapPut("/{id}/priority", [Authorize(Roles = "Admin")] async (string id, [FromBody] UpdateEmployeePriorityRequest request, ISender sender, ILogger<EmployeesModule> logger) =>
        {
            logger.LogInformation("📢 [API] UpdateEmployeePriority called for EmployeeId: {Id}, Priority: {Priority}", id, request.PriorityRating);

            var command = new UpdateEmployeePriorityCommand(id, request.PriorityRating);
            var result = await sender.Send(command);
            return Results.Ok(result);
        })
        .WithName("UpdateEmployeePriority")
        .WithOpenApi();
    }
}

public record UpdateEmployeePriorityRequest(int PriorityRating);
