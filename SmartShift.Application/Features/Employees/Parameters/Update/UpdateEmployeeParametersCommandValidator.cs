using FluentValidation;

namespace SmartShift.Application.Features.Employees.Parameters.Update;

public class UpdateEmployeeParametersCommandValidator : AbstractValidator<UpdateEmployeeParametersCommand>
{
    public UpdateEmployeeParametersCommandValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.SkillLevel).InclusiveBetween(1, 5);
        RuleFor(x => x.PriorityRating).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxShiftsPerWeek).GreaterThanOrEqualTo(0);
        // AdminNotes אופציונלי – אין כלל חובה
    }
}
