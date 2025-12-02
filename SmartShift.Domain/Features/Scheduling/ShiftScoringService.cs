using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using Microsoft.Extensions.Logging;
using System;

namespace SmartShift.Domain.Services;

public sealed class ShiftScoringService
{
    private readonly ILogger<ShiftScoringService> _logger;

    public ShiftScoringService(ILogger<ShiftScoringService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public sealed record ScoreBreakdown(
        double Availability,
        double Skill,
        double Priority,
        double WeeklyBalance,
        double Fairness,
        double Commitment,
        double AdminBoost)
    {
        public double Total =>
            Availability + Skill + Priority + WeeklyBalance + Fairness + Commitment + AdminBoost;
    }

    private static double Norm(double x, double max) =>
        Math.Clamp(x / Math.Max(1.0, max), 0.0, 1.0);

    public bool PassHardRules(
        Employee employee,
        Shift shift,
        int assignedThisWeek,
        EmployeeShiftAvailability arrivalType)
    {
        if (employee.SkillLevel < shift.SkillLevelRequired)
        {
            _logger.LogDebug(
                "Employee {Id} failed skill requirement: has {Has}, needs {Needs}",
                employee.Id, employee.SkillLevel, shift.SkillLevelRequired);
            return false;
        }

        if (employee.Gender == "Female" && arrivalType == EmployeeShiftAvailability.Early)
        {
            _logger.LogDebug(
                "Employee {Id} is female and cannot work Early shift",
                employee.Id);
            return false;
        }

        var weeklyLimit = Math.Min(employee.MaxShiftsPerWeek, 5);
        if (assignedThisWeek >= weeklyLimit)
        {
            _logger.LogDebug(
                "Employee {Id} reached weekly limit: {Assigned}/{Limit}",
                employee.Id, assignedThisWeek, weeklyLimit);
            return false;
        }

        return true;
    }

    public ScoreBreakdown CalculateScore(
        Employee employee,
        Shift shift,
        int assignedThisWeek,
        int assignedThisMonth,
        int teamMedianMonthly,
        int registeredThisWeekDesired,
        double adminReliabilityScore,
        double adminSkillDepthScore,
        double adminAttitudeScore)
    {
        _logger.LogInformation(
            "💯 Calculating score for Employee {Id} ({Name}) | AdminInput: R={R}, S={S}, A={A}",
            employee.Id,
            $"{employee.FirstName} {employee.LastName}",
            adminReliabilityScore,
            adminSkillDepthScore,
            adminAttitudeScore);

        var weeklyLimit = Math.Min(employee.MaxShiftsPerWeek, 5);

        // חישוב רכיבי הניקוד
        var availability = Math.Max(0, weeklyLimit - assignedThisWeek);
        var skillBoost = Math.Max(0, employee.SkillLevel - shift.SkillLevelRequired + 1);
        var priority = employee.PriorityRating;
        var weeklyBalance = Math.Max(0, weeklyLimit - assignedThisWeek);
        var fairness = Math.Max(0, teamMedianMonthly - assignedThisMonth);
        var commitmentSat = Norm(Math.Min(registeredThisWeekDesired, 5), 5);

        // ניקוד רגיל (לפני בוסט)
        var availabilityScore = 3.0 * Norm(availability, 5);
        var skillScore = 3.0 * Norm(skillBoost, 4);
        var priorityScore = 2.0 * Norm(priority, 10);
        var weeklyBalanceScore = 1.5 * Norm(weeklyBalance, 5);
        var fairnessScore = 0.5 * Norm(fairness, 10);
        var commitmentScore = 2.5 * commitmentSat;

        var beforeTotal =
            availabilityScore +
            skillScore +
            priorityScore +
            weeklyBalanceScore +
            fairnessScore +
            commitmentScore;

        _logger.LogInformation(
            "📊 Employee {Id} | Base Breakdown: Avail={A:F2}, Skill={S:F2}, Pri={P:F2}, Bal={B:F2}, Fair={F:F2}, Com={C:F2} → Total={T:F2}",
            employee.Id,
            availabilityScore, skillScore, priorityScore,
            weeklyBalanceScore, fairnessScore, commitmentScore,
            beforeTotal);

        // בוסט מהערות מנהל
        var adminBoostValue =
            (2.0 * Norm(adminReliabilityScore, 10)) +
            (1.0 * Norm(adminAttitudeScore, 10));

        _logger.LogInformation(
            "🚀 Employee {Id} | AdminBoost Calculation: (2.0 × {R}/10) + (1.0 × {A}/10) = {Boost:F2}",
            employee.Id,
            adminReliabilityScore,
            adminAttitudeScore,
            adminBoostValue);

        var finalTotal = beforeTotal + adminBoostValue;

        _logger.LogInformation(
            "✅ Employee {Id} | FINAL SCORE: {Before:F2} + {Boost:F2} = {Final:F2}",
            employee.Id, beforeTotal, adminBoostValue, finalTotal);

        return new ScoreBreakdown(
            Availability: availabilityScore,
            Skill: skillScore,
            Priority: priorityScore,
            WeeklyBalance: weeklyBalanceScore,
            Fairness: fairnessScore,
            Commitment: commitmentScore,
            AdminBoost: adminBoostValue);
    }
}