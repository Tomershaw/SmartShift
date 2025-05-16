using Carter;
using MediatR;
using SmartShift.Api.Requests;
using SmartShift.Application.Authentication.Login;

namespace SmartShift.Api.Endpoints;

public class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/login", async (
            LoginRequest request,
            IMediator mediator) =>
        {
            var command = new LoginCommand
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(new { Message = result.Message });
            }

            return Results.Ok(new { Message = result.Message, Token = result.Token });
        })
        .WithName("Login")
        .WithTags("Account");
    }
}
