using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartShift.Infrastructure.Authentication;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Authentication.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public LoginHandler(UserManager<ApplicationUser> userManager, IJwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return new LoginResult
            {
                Success = false,
                Message = "Invalid email or password."
            };
        }

        // Generate real JWT token
        var token = await _tokenGenerator.GenerateTokenAsync(user);



        return new LoginResult
        {
            Success = true,
            Message = "Login successful.",
            Token = token
        };
    }
}
