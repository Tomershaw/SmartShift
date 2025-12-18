using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Application.Features.Account.ResetPassword;

namespace SmartShift.Api.Endpoints.Account;

public sealed class ResetPasswordEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/reset-password", ResetPassword)
           .AllowAnonymous()
           .WithName("ResetPassword")
           .WithTags("Account");
    }

    [AllowAnonymous]
    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        IMediator mediator)
    {
        await mediator.Send(command);

        // לא מחזירים פרטים מסוכנים
        return Results.Ok(new
        {
            message = "If the token is valid, the password has been reset."
        });
    }
}
