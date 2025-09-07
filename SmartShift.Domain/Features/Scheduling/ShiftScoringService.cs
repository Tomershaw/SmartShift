using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Domain.Services;

public sealed class ShiftScoringService
{
    public sealed record ScoreBreakdown(
        double Availability,
        double Skill,
        double Priority,
        double WeeklyBalance,
        double Fairness,
        double Commitment)
    {
        public double Total => Availability + Skill + Priority + WeeklyBalance + Fairness + Commitment;
    }

    private static double Norm(double x, double max) => Math.Clamp(x / Math.Max(1.0, max), 0.0, 1.0);

    // חוקים קשיחים שחייבים לעמוד בהם
    public bool PassHardRules(Employee employee, Shift shift, int assignedThisWeek)
    {
        if (employee.SkillLevel < shift.SkillLevelRequired)
            return false;

        // תקרה שבועית א-ה: לא יותר מ 5 וגם לא יותר מ MaxShiftsPerWeek של העובד
        var weeklyLimit = Math.Min(employee.MaxShiftsPerWeek, 5);
        if (assignedThisWeek >= weeklyLimit)
            return false;

        return true;
    }

    // ניקוד כולל לעובד עבור משמרת
    public ScoreBreakdown CalculateScore(
        Employee employee,
        Shift shift,
        int assignedThisWeek,
        int assignedThisMonth,
        int teamMedianMonthly,
        int registeredThisWeekDesired)
    {
        var weeklyLimit = Math.Min(employee.MaxShiftsPerWeek, 5);

        var availability = Math.Max(0, weeklyLimit - assignedThisWeek);
        var skillBoost = Math.Max(0, employee.SkillLevel - shift.SkillLevelRequired + 1);
        var priority = employee.PriorityRating;
        var weeklyBalance = Math.Max(0, weeklyLimit - assignedThisWeek);
        var fairness = Math.Max(0, teamMedianMonthly - assignedThisMonth);

        // מחויבות: מתגמל מי שנרשם להרבה משמרות השבוע, עם רוויה ב 5
        var commitmentSat = Norm(Math.Min(registeredThisWeekDesired, 5), 5);

        return new ScoreBreakdown(
            Availability: 3.0 * Norm(availability, 5),
            Skill: 3.0 * Norm(skillBoost, 4),
            Priority: 2.0 * Norm(priority, 10),
            WeeklyBalance: 1.5 * Norm(weeklyBalance, 5),
            Fairness: 0.5 * Norm(fairness, 10),
            Commitment: 2.5 * commitmentSat
        );
    }
}
