using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartShift.Infrastructure.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmartShift.Domain.Data;

namespace SmartShift.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenGenerator(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
    }

    public async Task<string> GenerateTokenAsync(Domain.Data.ApplicationUser user)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
        var tokenHandler = new JwtSecurityTokenHandler();

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        // ✅ הוספת ה-Roles עם המפתח "roles" (לא ClaimTypes.Role)
        //   foreach (var role in roles)
        //    {
        //      claims.Add(new Claim("roles", role));
        //     }

        // ✅ הוספת ה-Roles עם ClaimTypes.Role (הפורמט הסטנדרטי)
        foreach (var role in roles)
        {
            //claims.Add(new Claim("role", role)); // ✅ נכון כך
            //claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role));

        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
