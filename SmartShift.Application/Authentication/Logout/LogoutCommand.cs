using MediatR;

namespace SmartShift.Application.Authentication.Logout;

public class LogoutCommand : IRequest<LogoutResult>
{
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}