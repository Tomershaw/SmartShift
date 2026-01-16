using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SmartShift.Application.Features.UserManagement.DeleteUser;

namespace SmartShift.Api.Endpoints;

public sealed class DeleteUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/users/{userId}", DeleteUser)
           .RequireAuthorization(policy => policy.RequireRole("Admin"))
           .WithName("DeleteUser")
           .WithTags("UserManagement");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> DeleteUser(
        string userId,
        ClaimsPrincipal user,
        IMediator mediator)
    {
        var tenantIdStr = user.FindFirst("tenantId")?.Value;
        if (string.IsNullOrWhiteSpace(tenantIdStr) || !Guid.TryParse(tenantIdStr, out var tenantId))
        {
            return Results.BadRequest(new { Message = "Tenant ID not found or invalid in token" });
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest(new { Message = "UserId is required" });
        }

        var cmd = new DeleteUserCommand
        {
            UserId = userId,
            TenantId = tenantId
        };

        var result = await mediator.Send(cmd);

        if (!result.Success)
        {
            return Results.BadRequest(new { result.Message, result.Errors });
        }

        return Results.Ok(new { Success = true, Message = "המשתמש נמחק בהצלחה" });
    }
}