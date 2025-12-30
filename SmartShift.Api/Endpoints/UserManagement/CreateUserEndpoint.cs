using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Api.Requests;
using SmartShift.Application.Features.UserManagement.CreateUser;

namespace SmartShift.Api.Endpoints;

public class CreateUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/create", CreateUser)
           .RequireAuthorization(policy => policy.RequireRole("Admin"))
           .WithName("CreateUser")
           .WithTags("UserManagement");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserRequest? request,
        IMediator mediator,
        HttpContext httpContext)
    {
        if (request is null)
            return Results.BadRequest(new { Message = "Invalid request body" });

        var adminTenantId = httpContext.User.FindFirst("tenantId")?.Value;
        if (string.IsNullOrEmpty(adminTenantId))
            return Results.BadRequest(new { Message = "Tenant ID not found in token" });

        if (!Guid.TryParse(adminTenantId, out var tenantGuid))
            return Results.BadRequest(new { Message = "Invalid Tenant ID format" });

        // ולידציה בסיסית כדי למנוע 500 בשל ערכים ריקים
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
        {
            return Results.BadRequest(new { Errors = new[] { "Email, Password, FullName are required" } });
        }

        var cmd = new CreateUserCommand
        {
            Email = request.Email,
            FullName = request.FullName,
            Password = request.Password,
            TenantId = tenantGuid,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? "Unknown" : request.Gender
        };

        var result = await mediator.Send(cmd);

        if (!result.Success)
            return Results.BadRequest(new { Errors = result.Errors, Message = result.Message });

        return Results.Ok(new { Success = true, Message = "המשתמש נוצר בהצלחה", UserId = result.UserId });
    }
}
