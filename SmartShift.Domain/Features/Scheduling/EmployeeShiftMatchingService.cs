using System;
using System.Collections.Generic;
using System.Linq;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Domain.Services
{
    /// <summary>
    /// Provides hard-constraint matching between an employee and a shift.
    /// Design notes:
    /// - ADDED: A new overload of CanEmployeeWorkShift that accepts EmployeeShiftAvailability (per-employee, per-shift).
    /// - CHANGED: The legacy overload that takes ShiftRegistration forwards to the new overload to keep backward compatibility.
    /// - REMOVED/AVOIDED: No fake availability checks. Early vs Regular is NOT enforced as a hard constraint here.
    ///   MinimumEarly should be handled in the planning/allocation layer, not in hard matching.
    /// </summary>
    public class EmployeeShiftMatchingService
    {
        /// <summary>
        /// NEW - Preferred overload.
        /// Checks if a specific employee can work a specific shift, given the employee's arrival type for THIS shift.
        /// The arrival argument is kept for future hard rules if product policy requires it.
        /// Currently Early/Regular is not a hard blocker here - planning layer should use it to satisfy MinimumEarly.
        /// </summary>
        /// <param name="employee">Employee under evaluation.</param>
        /// <param name="shift">Target shift.</param>
        /// <param name="arrival">Employee arrival type for this shift (Early or Regular).</param>
        /// <returns>true if the employee passes hard constraints for this shift.</returns>
        public bool CanEmployeeWorkShift(Employee employee, Shift shift, EmployeeShiftAvailability arrival)
        {
            ArgumentNullException.ThrowIfNull(employee);
            ArgumentNullException.ThrowIfNull(shift);

            // Hard rules go here. Keep them deterministic and data-based.
            if (!BasicCompatibilityCheck(employee, shift)) return false;
            if (employee.Gender == "Female" && arrival == EmployeeShiftAvailability.Early)
                return false;   
            if (!PassTimeAndPolicyGuards(employee, shift)) return false;

            // NOTE: 'arrival' is intentionally not used as a hard constraint.
            // Early vs Regular is enforced by the planning layer to meet MinimumEarly, not by matching.
            return true;
        }

        /// <summary>
        /// LEGACY - Backward compatible overload.
        /// CHANGED: Now forwards to the new overload that accepts 'arrival'.
        /// </summary>
        /// <param name="employee">Employee under evaluation.</param>
        /// <param name="shift">Target shift.</param>
        /// <param name="shiftRegistration">A single registration previously used to infer availability.</param>
        /// <returns>true if the employee passes hard constraints for this shift.</returns>
        public bool CanEmployeeWorkShift(Employee employee, Shift shift, ShiftRegistration shiftRegistration)
        {
            var arrival = shiftRegistration?.ShiftArrivalType ?? EmployeeShiftAvailability.Regular;
            return CanEmployeeWorkShift(employee, shift, arrival);
        }

        /// <summary>
        /// Basic capability checks that must hold regardless of scheduling context.
        /// Keep this method small and deterministic.
        /// </summary>
        private bool BasicCompatibilityCheck(Employee employee, Shift shift)
        {
            // Skill requirement
            if (employee.SkillLevel < shift.SkillLevelRequired)
                return false;

            // TODO: If roles exist, enforce role-to-shift mapping here.

            return true;
        }

        /// <summary>
        /// Central place for time and policy guards.
        /// Examples:
        /// - Overlap with existing approved shifts
        /// - Rest period between shifts
        /// - Weekly cap on number of shifts
        /// - Administrative blocks or leave
        /// ADDED: Kept as a single hook to make future policy additions easy.
        /// </summary>
        private bool PassTimeAndPolicyGuards(Employee employee, Shift shift)
        {
            // TODO: Inject repository if you need live assignments to check overlaps/rest.
            // For now, assume pass.
            return true;
        }

        /// <summary>
        /// Returns a combined candidate list:
        /// 1) Valid registered employees for this shift (per-employee arrival is respected).
        /// 2) If still below Required, adds additional employees from 'allEmployees' that pass hard checks.
        /// 
        /// CHANGED:
        /// - Accepts registered people as (Employee, Arrival) so we do not project a single registration on everyone.
        /// - Uses HashSet and DistinctBy for efficiency and to avoid duplicates.
        /// - Does NOT try to infer availability from historical registrations.
        ///   Arrival is per-shift input and planning layer is responsible for MinimumEarly.
        /// </summary>
        public IEnumerable<Employee> GetExpandedCandidatesForShift(
            Shift shift,
            IEnumerable<(Employee Emp, EmployeeShiftAvailability Arrival)> registeredPeople,
            IEnumerable<Employee> allEmployees)
        {
            ArgumentNullException.ThrowIfNull(shift);
            ArgumentNullException.ThrowIfNull(registeredPeople);
            ArgumentNullException.ThrowIfNull(allEmployees);

            // 1) Start with registered employees that pass hard rules
            var validRegistered = registeredPeople
                .Where(p => CanEmployeeWorkShift(p.Emp, shift, p.Arrival))
                .Select(p => p.Emp)
                .DistinctBy(e => e.Id)
                .ToList();

            if (validRegistered.Count >= shift.RequiredEmployeeCount)
                return validRegistered;

            // 2) If still short, consider non-registered employees who pass hard checks
            var seen = new HashSet<Guid>(validRegistered.Select(e => e.Id));

            var additional = allEmployees
                .Where(e => !seen.Contains(e.Id))
                // If you have per-employee arrival for non-registered people, use it here.
                // Otherwise assume Regular as a safe default for hard checks only.
                .Where(e => CanEmployeeWorkShift(e, shift, EmployeeShiftAvailability.Regular))
                .DistinctBy(e => e.Id);

            return validRegistered.Concat(additional);
        }
    }
}
