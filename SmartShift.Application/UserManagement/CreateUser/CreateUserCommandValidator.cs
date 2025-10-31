using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SmartShift.Domain.Data;

namespace SmartShift.Application.Features.UserManagement.CreateUser
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private static readonly string[] AllowedRoles = { "Admin", "Manager", "Employee" };
        private static readonly string[] AllowedGenders = { "Unknown", "Male", "Female", "Other" };

        private readonly IServiceProvider _serviceProvider;

        public CreateUserCommandValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Email
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("אימייל הוא שדה חובה")
                .EmailAddress().WithMessage("כתובת אימייל לא תקינה")
                .MustAsync(BeUniqueEmail).WithMessage("כתובת האימייל כבר קיימת במערכת");

            // Full name
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("שם מלא הוא שדה חובה")
                .MinimumLength(2).WithMessage("שם מלא קצר מדי")
                .MaximumLength(100).WithMessage("שם מלא ארוך מדי");

            // Password
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("סיסמה היא שדה חובה")
                .MinimumLength(6).WithMessage("הסיסמה חייבת להיות באורך של לפחות 6 תווים");
            // שים לב: למדיניות Identity יכולים להיות תנאים מחמירים יותר בפועל

            // TenantId
            RuleFor(x => x.TenantId)
                .Must(g => g != Guid.Empty).WithMessage("TenantId שגוי");

            // Role - רק מתוך הסט המותר
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("תפקיד הוא שדה חובה")
                .Must(r => AllowedRoles.Contains(r))
                .WithMessage("תפקיד לא חוקי. מותר: Admin, Manager, Employee");

            // Gender - אופציונלי; אם מולא, חייב להיות מתוך הסט
            RuleFor(x => x.Gender)
                .Must(g => string.IsNullOrWhiteSpace(g) || AllowedGenders.Contains(g))
                .WithMessage("Gender לא חוקי. מותר: Unknown, Male, Female, Other");

            // PhoneNumber - אופציונלי; אם מולא, נרמול ספרות בלבד ובדיקת אורך סביר
            RuleFor(x => x.PhoneNumber)
                .Must(p =>
                {
                    if (string.IsNullOrWhiteSpace(p)) return true; // אופציונלי
                    var digits = new string(p.Where(char.IsDigit).ToArray());
                    return digits.Length >= 9 && digits.Length <= 11;
                })
                .WithMessage("מספר טלפון חייב להכיל 9-11 ספרות");
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var trimmed = (email ?? string.Empty).Trim();
            var user = await userManager.FindByEmailAsync(trimmed); // Identity מטפל בנרמול
            return user == null;
        }
    }
}
