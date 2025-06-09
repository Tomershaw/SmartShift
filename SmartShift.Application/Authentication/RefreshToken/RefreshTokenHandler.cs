using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SmartShift.Application.Authentication.Refresh;
using SmartShift.Infrastructure.Data;
using System.Security.Cryptography;
using System.Net.Http;
using Azure.Core;
using SmartShift.Infrastructure.Authentication;


namespace SmartShift.Application.Authentication.Refresh;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly IJwtTokenGenerator _tokenGenerator;


    public RefreshTokenHandler(ApplicationDbContext dbContext, ILogger<RefreshTokenHandler> logger, IJwtTokenGenerator tokenGenerator)
    {
        _dbContext = dbContext;
        _logger = logger;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {

            var oldToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.Token == request.RefreshToken);


        if (oldToken == null || !oldToken.IsActive)
        {
            _logger.LogWarning("? Invalid or inactive refresh token");
            return new RefreshTokenResult
            {
                Success = false,
                Message = "Invalid or expired refresh token"
            };
        }

        // החלפת הטוקן במקום להוסיף חדש
        var newTokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        oldToken.ReplacedByToken = oldToken.Token;
        oldToken.Token = newTokenValue;
        oldToken.Expires = DateTime.UtcNow.AddDays(7);
        oldToken.Created = DateTime.UtcNow;
        oldToken.CreatedByIp = request.IpAddress;

        _logger.LogInformation("?? Refresh token updated to: {Token}", oldToken.Token);

        await _dbContext.SaveChangesAsync();



        var newAccessToken = await _tokenGenerator.GenerateTokenAsync(oldToken.User!);

        _logger.LogInformation("? Successfully refreshed token for user {UserId}", oldToken.UserId);

            
        return new RefreshTokenResult
        {
            Success = true,
            Message = "Token refreshed successfully",
            Token = newAccessToken,
            RefreshToken = newTokenValue
        };
    }

}
