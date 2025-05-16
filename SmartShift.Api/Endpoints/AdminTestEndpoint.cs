using Carter;
using Microsoft.AspNetCore.Authorization;

namespace SmartShift.Api.Endpoints;

public class AdminTestEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/test", [Authorize(Roles = "Admin")] () =>
        {
            return Results.Ok("🎉 You are authenticated as an Admin!");
        })
        .WithName("AdminTest")
        .WithTags("Admin");
    }
}
