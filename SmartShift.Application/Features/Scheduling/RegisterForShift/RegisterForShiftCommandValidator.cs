using FluentValidation;

namespace SmartShift.Application.Features.Scheduling.RegisterForShift;

public class RegisterForShiftCommandValidator : AbstractValidator<RegisterForShiftCommand>
{
    public RegisterForShiftCommandValidator()
    {
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
} 