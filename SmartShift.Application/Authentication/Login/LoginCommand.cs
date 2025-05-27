using MediatR;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SmartShift.Application.Authentication.Login;

public class LoginCommand : IRequest<LoginResult>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
