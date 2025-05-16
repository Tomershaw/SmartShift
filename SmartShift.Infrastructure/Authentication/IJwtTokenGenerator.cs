using SmartShift.Infrastructure.Data;


namespace SmartShift.Infrastructure.Authentication;
public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user);
}
