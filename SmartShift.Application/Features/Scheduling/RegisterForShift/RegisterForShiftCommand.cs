using MediatR;
using SmartShift.Domain.Features.Employees;

namespace SmartShift.Application.Features.Scheduling.RegisterForShift
{
    public class RegisterForShiftCommand : IRequest<RegisterForShiftResult>
    {
        public Guid ShiftId { get; set; }

        // ���� ������ - ����� ����� ����� ����� (Early �� Regular)
        public EmployeeShiftAvailability ShiftArrivalType { get; set; }

        // �� ���� ������ - ����� ������ ��� ��JWT
        public Guid UserId { get; set; }
    }
}
