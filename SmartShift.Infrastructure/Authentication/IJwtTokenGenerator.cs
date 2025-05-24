using SmartShift.Domain.Data;
using System.Threading.Tasks;

namespace SmartShift.Infrastructure.Authentication;

public interface IJwtTokenGenerator     
{
    Task<string> GenerateTokenAsync(ApplicationUser user);
}
