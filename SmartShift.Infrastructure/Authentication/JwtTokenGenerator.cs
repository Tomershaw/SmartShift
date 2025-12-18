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
using Microsoft.EntityFrameworkCore;

namespace SmartShift.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public JwtTokenGenerator(IConfiguration configuration, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _configuration = configuration;
        _userManager = userManager;
        _context = context;
    }

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);
        var tokenHandler = new JwtSecurityTokenHandler();
        var roles = await _userManager.GetRolesAsync(user);

        // ✅ מציאת Employee המקושר למשתמש
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.UserId == user.Id);

        // 🔥 הוסף לוגים!
        Console.WriteLine($"=== JWT DEBUG ===");
        Console.WriteLine($"User ID: {user.Id}");
        Console.WriteLine($"Employee found: {employee != null}");
        if (employee != null)
        {
            Console.WriteLine($"Employee Gender: '{employee.Gender}'");
            Console.WriteLine($"Gender is null or empty: {string.IsNullOrEmpty(employee.Gender)}");
        }
        Console.WriteLine($"=================");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("fullName", user.FullName)
        };

        // 🔥 הוסף Gender מה-Employee
        if (employee != null && !string.IsNullOrEmpty(employee.Gender))
        {
            claims.Add(new Claim(ClaimTypes.Gender, employee.Gender));
            Console.WriteLine($"✅ Added gender claim: {employee.Gender}");
        }
        else
        {
            Console.WriteLine($"❌ Gender NOT added!");
        }

        // ✅ הוספת EmployeeId אם קיים
        if (employee != null)
        {
            claims.Add(new Claim("employeeId", employee.Id.ToString()));
        }

        // ✅ הוספת TenantId
        if (user.TenantId.HasValue)
        {
            claims.Add(new Claim("tenantId", user.TenantId.Value.ToString()));
        }

        // ✅ הוספת ה-Roles
        foreach (var role in roles)
        {
            Console.WriteLine($"✅ Adding role claim: {role}");
            claims.Add(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role));
        }

        Console.WriteLine($"Total roles added: {roles.Count}");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
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