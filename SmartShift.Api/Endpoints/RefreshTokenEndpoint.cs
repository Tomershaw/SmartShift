using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShift.Application.Authentication.Refresh;
using SmartShift.Infrastructure.Authentication;
using SmartShift.Infrastructure.Data;
using System.Security.Cryptography;


namespace SmartShift.Api.Endpoints;

public class RefreshTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/refresh-token", async (
            [FromBody] RefreshTokenCommand command,
            HttpContext context,
            ISender mediator,
            ILogger<RefreshTokenEndpoint> logger) =>
        {


            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await mediator.Send(command);
            if (!result.Success)
            {
                logger.LogWarning("❌ Failed to refresh token: {Message}", result.Message);
                return Results.Unauthorized();
            }

            logger.LogInformation("✅ Token refreshed for user");


            return Results.Ok(new
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken
            });
        })
        .WithName("RefreshToken")
        .WithTags("Account");
    }
}

public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
}
