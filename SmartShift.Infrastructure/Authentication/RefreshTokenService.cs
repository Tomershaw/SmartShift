using System.Security.Cryptography;
using SmartShift.Domain.Features.RefreshTokens;

namespace SmartShift.Infrastructure.Authentication
{
    public class RefreshTokenService
    {
        public RefreshToken GenerateRefreshToken(string userId, string ipAddress)
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(randomBytes);

            return new RefreshToken
            {
                Token = token,
                Expires = DateTime.UtcNow.AddDays(7), // תקף לשבוע
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                UserId = userId
            };
        }
    }
}
