using Microsoft.AspNetCore.Identity;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartShift.Application.Features.UserManagement.CreateUser
{
    public class UserRegistrationService : IUserRegistrationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUser;

        // ערכי gender מותרים
        private static readonly string[] AllowedGenders = { "Unknown", "Male", "Female", "Other" };

        public UserRegistrationService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        public async Task<CreateUserResult> RegisterUserAsync(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // נרמול קלטים
            var email = (request.Email ?? string.Empty).Trim();
            var fullName = (request.FullName ?? string.Empty).Trim();
            var role = string.IsNullOrWhiteSpace(request.Role) ? "Employee" : request.Role.Trim();
            var phone = (request.PhoneNumber ?? string.Empty).Trim();

            // אימייל כפול
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new CreateUserResult
                {
                    Success = false,
                    Message = "Duplicate email",
                    Errors = new[] { "כבר קיים משתמש עם כתובת האימייל הזו" }
                };
            }

            // תפקיד חייב להיות קיים
            var roleExists = await _roleManager.RoleExistsAsync(role);
            if (!roleExists)
            {
                return new CreateUserResult
                {
                    Success = false,
                    Message = $"Role '{role}' not found",
                    Errors = new[] { $"התפקיד '{role}' לא קיים במערכת. ודא שנזרעו התפקידים Admin, Manager, Employee." }
                };
            }

            // TenantId מהטוקן של המנהל
            var tenantId = request.TenantId;
            if (tenantId == Guid.Empty)
            {
                return new CreateUserResult
                {
                    Success = false,
                    Message = "Tenant ID is missing",
                    Errors = new[] { "Tenant ID not found in token" }
                };
            }

            // יצירת משתמש ב-Identity
            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
                TenantId = tenantId
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
            {
                return new CreateUserResult
                {
                    Success = false,
                    Message = "Identity create failed",
                    Errors = createResult.Errors.Select(e => e.Description).ToArray()
                };
            }

            // שיוך תפקיד
            var addRoleResult = await _userManager.AddToRoleAsync(newUser, role);
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                return new CreateUserResult
                {
                    Success = false,
                    Message = $"AddToRole failed: {role}",
                    Errors = addRoleResult.Errors.Select(e => e.Description).ToArray()
                };
            }

            // יצירת Employee תואם
            try
            {
                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var firstName = parts.Length > 0 ? parts[0] : fullName;
                var lastName = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;

                var genderFromReq = (request.Gender ?? "Unknown").Trim();
                if (!AllowedGenders.Contains(genderFromReq, StringComparer.OrdinalIgnoreCase))
                    genderFromReq = "Unknown";
                genderFromReq = AllowedGenders.First(g =>
                    string.Equals(g, genderFromReq, StringComparison.OrdinalIgnoreCase));

                var phoneDigits = string.IsNullOrWhiteSpace(phone)
                    ? string.Empty
                    : new string(phone.Where(char.IsDigit).ToArray());

                var employee = new Employee(
                    firstName: firstName,
                    lastName: lastName,
                    email: email,
                    phoneNumber: phoneDigits,
                    priorityRating: 1
                )
                {
                    TenantId = tenantId,
                    UserId = newUser.Id,
                    Gender = genderFromReq
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
                    Message = "Create Employee failed",
                    Errors = new[] { $"נכשלה יצירת רשומת העובד: {ex.Message}" }
                };
            }

            return new CreateUserResult
            {
                Success = true,
                UserId = newUser.Id,
                TenantId = tenantId,
                Message = "המשתמש והעובד נוצרו בהצלחה"
            };
        }
    }
}
