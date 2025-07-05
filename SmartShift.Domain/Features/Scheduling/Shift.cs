using SmartShift.Domain.Data;
using SmartShift.Domain.Features.ShiftRegistrations;
using System;

namespace SmartShift.Domain.Features.Scheduling;

public class Shift
{
    public Guid Id { get; private set; }
    public DateTime StartTime { get; private set; }
   // public DateTime EndTime { get; private set; }
    public int RequiredEmployeeCount { get; private set; } // מספר העובדים הנדרש
    public int MinimumEmployeeCount { get; private set; } // מספר העובדים המינימלי
   // public string WorkType { get; private set; } // סוג עבודה
    public int SkillLevelRequired { get; private set; } // רמת מיומנות נדרשת
    public string Description { get; private set; } // תיאור המשמרת
    public Guid? AssignedEmployeeId { get; private set; }
    public ShiftStatus Status { get; private set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public ICollection<ShiftRegistration> ShiftRegistrations { get; set; } = new List<ShiftRegistration>();


    private Shift() { } // For EF Core

    public Shift(DateTime startTime, int requiredEmployeeCount, int minimumEmployeeCount, int skillLevelRequired, string description)
    {

        if (requiredEmployeeCount < 1)
            throw new ArgumentException("Required employee count must be at least 1");

        if (minimumEmployeeCount < 0 || minimumEmployeeCount > requiredEmployeeCount)
            throw new ArgumentException("Minimum employee count must be between 0 and the required employee count");

        Id = Guid.NewGuid();
        StartTime = startTime;
        RequiredEmployeeCount = requiredEmployeeCount;
        MinimumEmployeeCount = minimumEmployeeCount;
        SkillLevelRequired = skillLevelRequired;
        Description = description;
        Status = ShiftStatus.Open;
    }

    public void AssignEmployee(Guid employeeId)
    {
        AssignedEmployeeId = employeeId;
        Status = ShiftStatus.Assigned;
    }

    public void UnassignEmployee()
    {
        AssignedEmployeeId = null;
        Status = ShiftStatus.Open;
    }

    public void Cancel()
    {
        Status = ShiftStatus.Cancelled;
    }
}
