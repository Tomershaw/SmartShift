using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartShift.Application.Features.Scheduling.Commands.RegisterForShift
{
    public class RegisterForShiftCommandValidator : AbstractValidator<RegisterForShiftCommand>
    {
        public RegisterForShiftCommandValidator()
        {
            RuleFor(x => x.ShiftId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
