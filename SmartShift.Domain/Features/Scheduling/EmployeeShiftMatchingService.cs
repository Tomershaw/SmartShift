using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Domain.Services;

public class EmployeeShiftMatchingService
{
    /// <summary>
    /// בדיקה בסיסית האם עובד יכול לעבוד במשמרת נתונה
    /// </summary>
    /// <param name="employee">העובד</param>
    /// <param name="shift">המשמרת</param>
    /// <returns>true אם העובד מתאים למשמרת</returns>
    public bool CanEmployeeWorkShift(Employee employee, Shift shift)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(shift);

        return BasicCompatibilityCheck(employee, shift);
    }

    /// <summary>
    /// בדיקת תאימות בסיסית
    /// </summary>
    private bool BasicCompatibilityCheck(Employee employee, Shift shift)
    {
        // בדיקת רמת מיומנות
        if (employee.SkillLevel < shift.SkillLevelRequired)
            return false;

        // בדיקת סוג עבודה - רק אם לעובד יש סוגי עבודה מוגדרים
     //   if (employee.WorkTypes.Any() && !employee.WorkTypes.Contains(shift.WorkType))
       //    return false;

        return true;
    }
}