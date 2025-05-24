using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartShift.Domain.Data;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Authentication.Register;

public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new RegisterResult
            {
                Success = false,
                Message = "User with this email already exists"
            };
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return new RegisterResult
            {
                Success = false,
                Message = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        return new RegisterResult
        {
            Success = true,
            Message = "User registered successfully"
        };
    }
} 