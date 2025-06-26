using Microsoft.AspNetCore.Identity;
using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartShift.Application.Features.UserManagement.CreateUser;

public class UserRegistrationService : IUserRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public UserRegistrationService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<CreateUserResult> RegisterUserAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new CreateUserResult
            {
                Success = false,
                Errors = new[] { "כבר קיים משתמש עם כתובת האימייל הזו" }
            };
        }

        var newUser = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true,
            TenantId = request.TenantId
        };

        var result = await _userManager.CreateAsync(newUser, request.Password);
        if (!result.Succeeded)
        {
            return new CreateUserResult
            {
                Success = false,
                Errors = result.Errors.Select(e => e.Description).ToArray()
            };
        }

        var roleResult = await _userManager.AddToRoleAsync(newUser, request.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(newUser);
            return new CreateUserResult
            {
                Success = false,
                Errors = roleResult.Errors.Select(e => e.Description).ToArray()
            };
        }

        try
        {
            var nameParts = request.FullName.Split(' ');
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

            var employee = new Employee(
                firstName: firstName,
                lastName: lastName,
                email: request.Email,
                phoneNumber: request.PhoneNumber,
                priorityRating: 1
            )
            {
                TenantId = request.TenantId
            };

            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(newUser);
            return new CreateUserResult
            {
                Success = false,
                Errors = new[] { $"נכשלה יצירת רשומת העובד: {ex.Message}" }
            };
        }

        return new CreateUserResult
        {
            Success = true,
            UserId = newUser.Id,
            TenantId = request.TenantId,
            Message = "המשתמש והעובד נוצרו בהצלחה"
        };
    }
}
