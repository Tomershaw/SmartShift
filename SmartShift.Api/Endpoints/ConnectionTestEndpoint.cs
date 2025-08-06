using Carter;
using Microsoft.AspNetCore.Http;

namespace SmartShift.Api.Endpoints;

/// <summary>
/// Endpoint לאימות חיבור מוצלח למערכת SmartShift
/// </summary>
public class ConnectionTestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/connection-test", async (HttpContext context) =>
        {
            var response = new
            {
                Message = "✅ החיבור לפרוייקט SmartShift מוצלח!",
                ProjectName = "SmartShift - מערכת ניהול משמרות",
                Technology = ".NET 9.0 + React 19",
                Language = "עברית",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                Status = "Connected",
                Developer = "AI Assistant מחובר ומוכן לעזור! 🤖"
            };

            return Results.Ok(response);
        })
        .WithName("ConnectionTest")
        .WithTags("System")
        .WithSummary("בדיקת חיבור למערכת")
        .WithDescription("endpoint לאימות שהמערכת פועלת ומחוברת כראוי");
    }
}