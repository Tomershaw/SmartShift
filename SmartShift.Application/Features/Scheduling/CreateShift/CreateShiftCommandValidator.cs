// SmartShift.Application/Features/Scheduling/CreateShift/CreateShiftCommandValidator.cs
using FluentValidation;

namespace SmartShift.Application.Features.Scheduling.CreateShift;

public sealed class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftCommandValidator()
    {
        RuleFor(x => x.Payload).NotNull().WithMessage("Payload is required");

        When(x => x.Payload != null, () =>
        {
            RuleFor(x => x.Payload.Name)
                .NotEmpty().WithMessage("שם המשמרת נדרש")
                .MaximumLength(100).WithMessage("שם המשמרת לא יכול להיות ארוך מ-100 תווים");

            RuleFor(x => x.Payload.StartTime)
                .GreaterThan(DateTime.Now).WithMessage("זמן התחלה חייב להיות בעתיד");

            RuleFor(x => x.Payload.RequiredEmployeeCount)
                .GreaterThan(0).WithMessage("מספר עובדים נדרש חייב להיות גדול מ-0");

            RuleFor(x => x.Payload.MinimumEmployeeCount)
                .GreaterThanOrEqualTo(0).WithMessage("מספר עובדים מינימלי חייב להיות 0 או יותר")
                .LessThanOrEqualTo(x => x.Payload.RequiredEmployeeCount)
                .WithMessage("מספר עובדים מינימלי לא יכול להיות גדול ממספר העובדים הנדרש");

            RuleFor(x => x.Payload.MinimumEarlyEmployees)
                .GreaterThanOrEqualTo(0).WithMessage("מספר עובדי הקמה מינימלי חייב להיות 0 או יותר")
                .LessThanOrEqualTo(x => x.Payload.RequiredEmployeeCount)
                .WithMessage("מספר עובדי הקמה מינימלי לא יכול להיות גדול ממספר העובדים הנדרש");

            RuleFor(x => x.Payload.SkillLevelRequired)
                .InclusiveBetween(1, 10).WithMessage("רמת מיומנות חייבת להיות בין 1 ל-10");

            RuleFor(x => x.Payload.Description)
                .NotEmpty().WithMessage("תיאור המשמרת נדרש")
                .MaximumLength(500).WithMessage("תיאור המשמרת לא יכול להיות ארוך מ-500 תווים");
        });
    }
}
