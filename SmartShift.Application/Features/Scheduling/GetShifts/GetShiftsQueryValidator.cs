using FluentValidation;

namespace SmartShift.Application.Features.Scheduling.GetShifts;

public class GetShiftsQueryValidator : AbstractValidator<GetShiftsQuery>
{
    public GetShiftsQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("תאריך התחלה הוא שדה חובה")
            .Must(BeValidDate)
            .WithMessage("תאריך התחלה חייב להיות בפורמט תקין");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("תאריך סיום הוא שדה חובה")
            .Must(BeValidDate)
            .WithMessage("תאריך סיום חייב להיות בפורמט תקין");

        RuleFor(x => x)
            .Must(x => DateTime.TryParse(x.StartDate, out var start) &&
                      DateTime.TryParse(x.EndDate, out var end) &&
                      start <= end)
            .WithMessage("תאריך התחלה חייב להיות לפני או שווה לתאריך הסיום")
            .When(x => BeValidDate(x.StartDate) && BeValidDate(x.EndDate));

        RuleFor(x => x)
            .Must(x => DateTime.TryParse(x.StartDate, out var start) &&
                      DateTime.TryParse(x.EndDate, out var end) &&
                      (end - start).TotalDays <= 365)
            .WithMessage("טווח התאריכים לא יכול לעלות על 365 ימים")
            .When(x => BeValidDate(x.StartDate) && BeValidDate(x.EndDate));

        RuleFor(x => x)
            .Must(x => DateTime.TryParse(x.StartDate, out var start) && 
                      start >= DateTime.Today.AddYears(-1))
            .WithMessage("תאריך התחלה לא יכול להיות יותר משנה אחורה")
            .When(x => BeValidDate(x.StartDate));

        RuleFor(x => x)
            .Must(x => DateTime.TryParse(x.EndDate, out var end) &&
                      end <= DateTime.Today.AddYears(1))
            .WithMessage("תאריך סיום לא יכול להיות יותר משנה קדימה")
            .When(x => BeValidDate(x.EndDate));
    }

    private bool BeValidDate(string date)
    {
        return DateTime.TryParse(date, out _);
    }
}