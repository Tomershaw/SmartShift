using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Authentication.Logout;

public class LogoutHandler : IRequestHandler<LogoutCommand, LogoutResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(ApplicationDbContext dbContext, ILogger<LogoutHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 LogoutHandler started for user {UserId}", request.UserId);

        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == request.UserId && t.Revoked == null && t.Expires > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("🔍 Found {Count} tokens to revoke", tokens.Count);

        foreach (var token in tokens)
        {
            token.Revoked = DateTime.UtcNow;
            token.RevokedByIp = request.IpAddress;
         }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✅ Revoked {Count} tokens for user {UserId}", tokens.Count, request.UserId);

        return new LogoutResult
        {
            Success = true,
            RevokedCount = tokens.Count,
            Message = $"✅ Logout successful. {tokens.Count} tokens revoked."
        };
    }
}
