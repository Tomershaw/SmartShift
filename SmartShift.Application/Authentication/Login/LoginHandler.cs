using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartShift.Infrastructure.Authentication;
using SmartShift.Domain.Data;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Authentication.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly ApplicationDbContext _dbContext;
    private readonly RefreshTokenService _refreshTokenService;

    public LoginHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator tokenGenerator,
    RefreshTokenService refreshTokenService,
    ApplicationDbContext dbContext)
{
    _userManager = userManager;
    _tokenGenerator = tokenGenerator;
    _refreshTokenService = refreshTokenService;
    _dbContext = dbContext;
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
        var refreshToken = _refreshTokenService.GenerateRefreshToken(user.Id, "127.0.0.1");
       _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);



        return new LoginResult
           {
           Success = true,
           Message = "Login successful.",
           Token = token,
           RefreshToken = refreshToken.Token
           };
    }
}
