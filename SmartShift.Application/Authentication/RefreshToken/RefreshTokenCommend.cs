using MediatR;
using SmartShift.Application.Authentication.Refresh;

public class RefreshTokenCommand : IRequest<RefreshTokenResult>
{
    public string IpAddress { get; set; } = string.Empty; 
     public string RefreshToken { get; set; } = string.Empty;
     
}