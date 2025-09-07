using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Infrastructure.AI;

public interface IShiftAssignmentAIService
{
    // המתודות החדשות - עם מידע מדויק על כל עובד וסוג ההגעה שלו
    Task<string> AnalyzeShiftRequirementsAsync( Shift shift,IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> people, CancellationToken cancellationToken = default);

    Task<string> GenerateShiftSummaryAsync(Shift shift,IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> assignedPeople,CancellationToken cancellationToken = default);

    // זו נשארת בלי שינוי - לא נוגעים בה כרגע
    Task<IEnumerable<Employee>> GetRecommendedEmployeesAsync( Shift shift,IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> people, CancellationToken cancellationToken = default);

}
