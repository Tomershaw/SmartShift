using MediatR;
using SmartShift.Domain.Features.Employees;

namespace SmartShift.Application.Features.Scheduling.RegisterForShift
{
    public class RegisterForShiftCommand : IRequest<RegisterForShiftResult>
    {
        public Guid ShiftId { get; set; }

        // נשלח מהלקוח - בחירת העובד לאותה הרשמה (Early או Regular)
        public EmployeeShiftAvailability ShiftArrivalType { get; set; }

        // לא נשלח מהלקוח - מוזרק במודול לפי ה־JWT
        public Guid UserId { get; set; }
    }
}
