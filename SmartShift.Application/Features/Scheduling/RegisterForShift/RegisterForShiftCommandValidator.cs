using FluentValidation;
using SmartShift.Domain.Features.Employees;

namespace SmartShift.Application.Features.Scheduling.RegisterForShift
{
    public class RegisterForShiftCommandValidator : AbstractValidator<RegisterForShiftCommand>
    {
        public RegisterForShiftCommandValidator()
        {
            RuleFor(x => x.ShiftId)
                .NotEmpty().WithMessage("ShiftId is required");

            RuleFor(x => x.ShiftArrivalType)
                .IsInEnum().WithMessage("ShiftArrivalType must be a valid value (Early or Regular)");
        }
    }
}
