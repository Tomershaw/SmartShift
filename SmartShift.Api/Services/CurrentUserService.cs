using SmartShift.Application.Common.Interfaces;
using System.Security.Claims;

namespace SmartShift.Api.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found.");
        }


        public Guid GetTenantId()
        {
            var tenantIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("tenantId")?.Value;

            if (!Guid.TryParse(tenantIdStr, out var tenantId))
                throw new UnauthorizedAccessException("Tenant ID not found or invalid.");

            return tenantId;
        }
    }
}
