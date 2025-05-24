using Carter;
using Microsoft.EntityFrameworkCore;
using SmartShift.Infrastructure.Authentication;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Api.Endpoints;

public class RefreshTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/refresh-token", async (
            RefreshTokenRequest tokenRequest,
            HttpContext context,
            ApplicationDbContext dbContext,
            RefreshTokenService refreshTokenService,
            IJwtTokenGenerator tokenGenerator) =>
        {
            if (string.IsNullOrEmpty(tokenRequest.RefreshToken))
            {
                return Results.BadRequest(new { Message = "Missing refresh token" });
            }

            var oldToken = await dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == tokenRequest.RefreshToken);

            if (oldToken == null || !oldToken.IsActive)
            {
                return Results.Unauthorized();
            }

            // קבלת כתובת ה-IP האמיתית של המשתמש
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // ביטול הטוקן הישן
            oldToken.Revoked = DateTime.UtcNow;
            oldToken.RevokedByIp = ipAddress;

            // יצירת טוקן חדש
            var newRefreshToken = refreshTokenService.GenerateRefreshToken(oldToken.UserId, ipAddress);
            oldToken.ReplacedByToken = newRefreshToken.Token;

            // שמירה במסד
            dbContext.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync();

            if (oldToken.User == null)
             {
               return Results.Problem("Internal error: user not loaded.");
             }

            // הפקת JWT חדש
            var newAccessToken = await tokenGenerator.GenerateTokenAsync(oldToken.User);

            // תשובה
            return Results.Ok(new
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token
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
