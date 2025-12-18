using MediatR;

namespace SmartShift.Application.Features.Account.ForgotPassword;


public sealed record ForgotPasswordCommand(string Email) : IRequest;
