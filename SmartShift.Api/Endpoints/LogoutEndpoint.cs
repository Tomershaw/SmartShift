using Carter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SmartShift.Infrastructure.Data;
using System.Security.Claims;

namespace SmartShift.Api.Endpoints;

public class LogoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/logout", [Authorize] async (
            ClaimsPrincipal user,
            ApplicationDbContext dbContext,
            ILogger<LogoutEndpoint> logger) => // ✅ מוסיפים logger
        {
            try
            {
                logger.LogInformation("🚀 Logout endpoint reached");

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    logger.LogWarning("❌ userId missing in token");
                    return Results.Unauthorized();
                }

                logger.LogInformation("✅ Logout called by user {UserId}", userId);

                logger.LogInformation("🚀 לפני הטעינה מהמסד");

                 var tokens = await dbContext.RefreshTokens
                   .Where(t => t.UserId == userId && t.Revoked == null && t.Expires > DateTime.UtcNow)
                   .ToListAsync();

                logger.LogInformation("✅ טוקנים נטענו ({Count})", tokens.Count);

                foreach (var token in tokens)
                {
                    logger.LogInformation("🔁 מטפל בטוקן ID={Id}", token.Id);
                    token.Revoked = DateTime.UtcNow;
                    token.RevokedByIp = "logout-test";
                }

                logger.LogInformation("💾 לפני שמירה למסד");

                await dbContext.SaveChangesAsync();

                logger.LogInformation("✅ שמירה הצליחה");

                return Results.Ok(new
                {
                    Message = $"✅ Logout successful. {tokens.Count} tokens revoked.",
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Logout failed");
                return Results.Problem("Internal server error during logout.");
            }
        })
        .WithName("Logout")
        .WithTags("Account");
    }
}
