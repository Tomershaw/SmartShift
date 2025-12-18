using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SmartShift.Domain.Data;
using System.Net;

namespace SmartShift.Application.Features.Account.ResetPassword;

public sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task Handle(
     ResetPasswordCommand request,
     CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            // אנחנו לא רוצים לגלות לתוקפים אם המייל קיים, אבל אנחנו חייבים שהפעולה תסתיים
            throw new Exception("User not found");
        }

        // הוספתי החלפה ידנית של רווחים לפלוסים - בעיה נפוצה מאוד בטוקנים של Identity
        var fixedToken = request.Token.Replace(" ", "+");

        var result = await _userManager.ResetPasswordAsync(
            user,
            fixedToken, // משתמשים בטוקן המתוקן בלי WebUtility
            request.NewPassword);

        if (!result.Succeeded)
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("ResetPassword failed: {Errors}", errorMessages);

            // במקום return, אנחנו זורקים שגיאה!
            throw new InvalidOperationException($"Password reset failed: {errorMessages}");
        }

        _logger.LogInformation("Password successfully reset for user {Email}", request.Email);
    }
}
