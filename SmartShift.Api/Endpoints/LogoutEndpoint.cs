using Carter;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MediatR;
using SmartShift.Application.Authentication.Logout;

namespace SmartShift.Api.Endpoints;

public class LogoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/logout", [Authorize] async (
            ClaimsPrincipal user,
            HttpContext context,
            ISender mediator,
            ILogger<LogoutEndpoint> logger) =>
        {
            logger.LogInformation("🚀 Logout endpoint reached");

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("❌ userId missing in token");
                return Results.Unauthorized();
            }

            var command = new LogoutCommand
            {
                UserId = userId,
                IpAddress = ipAddress
            };

            try
            {
                var result = await mediator.Send(command);

                if (!result.Success)
                {
                    return Results.Problem(result.Message);
                }

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Logout failed with exception");
                return Results.Problem("Internal server error during logout.");
            }
        })
        .WithName("Logout")
        .WithTags("Account");
    }
}
