using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SmartShift.Domain.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartShift.Application.Features.UserManagement.CreateUser
{
    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private readonly IServiceProvider _serviceProvider;

        public CreateUserCommandValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // בדיקת תקינות האימייל
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("אימייל הוא שדה חובה")
                .EmailAddress().WithMessage("כתובת אימייל לא תקינה")
                .MustAsync(BeUniqueEmail).WithMessage("כתובת האימייל כבר קיימת במערכת");

            // בדיקת שם מלא
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("שם מלא הוא שדה חובה");

            // בדיקת סיסמה
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("סיסמה היא שדה חובה")
                .MinimumLength(6).WithMessage("הסיסמה חייבת להיות באורך של לפחות 6 תווים");

            // בדיקת Tenant ID
            RuleFor(x => x.TenantId)
                .NotEmpty().WithMessage("טננט ID הוא שדה חובה");

            // בדיקת תפקיד
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("תפקיד הוא שדה חובה");

            // הוספת בדיקה לשדה PhoneNumber
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("מספר טלפון הוא שדה חובה")
                .Matches(@"^(\+?\d{1,3}[-. ]?)?\(?([0-9]{2,3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$") // דוגמה לפורמט תקין
                .WithMessage("פורמט מספר הטלפון אינו תקין (לדוגמה: 055-1234567)");
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellation)
        {
            // Create a new scope
            using var scope = _serviceProvider.CreateScope();
            // Get UserManager from the new scope
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(email);
            return user == null;
        }
    }
}