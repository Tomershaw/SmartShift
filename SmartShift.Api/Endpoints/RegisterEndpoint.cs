using Carter;
using MediatR;
using SmartShift.Api.Requests;
using SmartShift.Application.Authentication.Register;


namespace SmartShift.Api.Endpoints;

public class RegisterEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/register", async (
            RegisterRequest request,
            IMediator mediator) =>
        {
            var command = new RegisterCommand
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password
            };

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(new { Message = result.Message });
            }

            return Results.Ok(new { Message = result.Message });
        })
        .WithName("Register")
        .WithTags("Account");
    }
}
