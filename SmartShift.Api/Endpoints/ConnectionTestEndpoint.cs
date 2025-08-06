using Carter;
using Microsoft.AspNetCore.Authorization;

namespace SmartShift.Api.Endpoints;

public class ConnectionTestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/connection/test", [AllowAnonymous] () =>
        {
            return Results.Ok(new { 
                message = "כן, אני מחובר לפרוייקט שלך! 🔗✅",
                englishMessage = "Yes, I am connected to your project! 🔗✅",
                projectName = "SmartShift",
                status = "connected",
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
            });
        })
        .WithName("ConnectionTest")
        .WithTags("Connection")
        .WithSummary("בודק חיבור לפרוייקט - Tests connection to the project")
        .WithDescription("נקודת קצה לבדיקת חיבור לפרוייקט SmartShift - Endpoint to test connection to SmartShift project");
    }
}