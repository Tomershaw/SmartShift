using Carter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartShift.Api.Endpoints;

public class TokenValidationEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/account/validate-token", [Authorize] () =>
        {
            return Results.Ok(new { Message = "Token is valid." });
        })
        .WithName("ValidateToken")
        .WithTags("Account");
    }
}
