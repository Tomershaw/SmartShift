using FluentValidation;
using SmartShift.Api.Features.Employees.Endpoints;

namespace SmartShift.Api.Features.Employees.Validators;

public class UpdateEmployeePriorityRequestValidator : AbstractValidator<UpdateEmployeePriorityRequest>
{
    public UpdateEmployeePriorityRequestValidator()
    {
        RuleFor(x => x.PriorityRating)
            .InclusiveBetween(1, 5)
            .WithMessage("Priority rating must be between 1 and 5");
    }
} 