using Carter;
using MediatR;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Api.Requests;
using SmartShift.Application.Features.Account.ForgotPassword;

namespace SmartShift.Api.Endpoints.Account;

public sealed class ForgotPasswordEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/account/forgot-password", ForgotPassword)
           .AllowAnonymous()
           .WithName("ForgotPassword")
           .WithTags("Account");
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest? request,
        IMediator mediator)
    {
        if (request is null)
            return Results.BadRequest(new { Message = "Invalid request body" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { Errors = new[] { "Email is required" } });

        // לא חושפים אם המשתמש קיים או לא
        await mediator.Send(new ForgotPasswordCommand(request.Email));

        return Results.Ok(new
        {
            Message = "If the email exists, you will receive a reset link."
        });
    }
}
