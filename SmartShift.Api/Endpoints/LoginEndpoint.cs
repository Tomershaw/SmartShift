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
            HttpContext http,
            IMediator mediator) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString();
            var command = new LoginCommand
            {
                Email = request.Email,
                Password = request.Password,
                IpAddress = ip ?? "unknown"
            };

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    Message = result.Message
                });
            }

            return Results.Ok(new
            {
                Message = result.Message,
                Token = result.Token,
                RefreshToken = result.RefreshToken
            });
        })
        .WithName("Login")
        .WithTags("Account");
    }
}
