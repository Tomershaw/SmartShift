using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartShift.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    // Accessible to everyone, no authentication required
    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult GetPublicMessage()
    {
        return Ok("✅ This is a public endpoint. No authentication required.");
    }

    // Requires a valid JWT token
    [Authorize]
    [HttpGet("secure")]
    public IActionResult GetSecureMessage()
    {
        return Ok("🔐 You are authenticated with a valid JWT!");
    }

    // Requires a valid JWT token AND the user to have the 'Admin' role
    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public IActionResult GetAdminMessage()
    {
        return Ok("👑 Welcome Admin! You have access to this resource.");
    }
}
