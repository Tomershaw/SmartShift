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
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
        }

        public Guid GetTenantId()
        {
            var tenantIdString = _httpContextAccessor.HttpContext?.User?.FindFirst("tenantId")?.Value;

            if (string.IsNullOrEmpty(tenantIdString))
                throw new UnauthorizedAccessException("Tenant ID not found in token.");

            return Guid.TryParse(tenantIdString, out var tenantId)
                ? tenantId
                : throw new UnauthorizedAccessException("Tenant ID format is invalid.");
        }
    }
}
