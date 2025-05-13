using FluentValidation;
using SmartShift.Application.Features.Scheduling.Commands.AssignEmployee;

namespace SmartShift.Api.Validators;

public class AssignEmployeeRequestValidator : AbstractValidator<AssignEmployeeCommand>
{
    public AssignEmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");
        RuleFor(x => x.ShiftId)
            .NotEmpty()
            .WithMessage("Shift Id is required");
    }
} 