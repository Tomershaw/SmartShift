using Carter;
using Microsoft.AspNetCore.Identity;
using SmartShift.Infrastructure.Data;
using SmartShift.Api.Requests;

namespace SmartShift.Api.Endpoints;

public class RegisterEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            // Check if user with email already exists
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Results.BadRequest(new { Message = "User with this email already exists." });
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return Results.BadRequest(new { Message = "Failed to create user.", Errors = result.Errors });
            }

            return Results.Ok(new { Message = "User registered successfully." });
        })
        .WithName("Register")
        .WithTags("Account");
    }
} 