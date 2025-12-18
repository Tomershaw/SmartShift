using MediatR;

namespace SmartShift.Application.Features.Account.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest;
