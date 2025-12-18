using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartShift.Domain.Data;
using SmartShift.Infrastructure.Interfaces;
using System.Net;

namespace SmartShift.Application.Features.Account.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<ForgotPasswordCommandHandler> logger,
        IConfiguration configuration,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
        _emailSender = emailSender;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            _logger.LogInformation(
                "ForgotPassword requested for non-existing email: {Email}",
                request.Email);

            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var encodedToken = WebUtility.UrlEncode(token);
        var encodedEmail = WebUtility.UrlEncode(request.Email);

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";

        var resetLink = $"{frontendBaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

        var subject = "Reset your SmartShift password";
        var html = $@"
        <p>Hi,</p>
        <p>Click the link to reset your password:</p>
        <p><a href=""{resetLink}"">Reset password</a></p>
        ";

        await _emailSender.SendAsync(request.Email, subject, html);

        _logger.LogInformation("✅ ForgotPassword email sent (if address exists): {Email}", request.Email);
    }
}
