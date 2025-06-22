using SmartShift.Domain.Data;
using System;

namespace SmartShift.Domain.Features.Scheduling;

public class Shift
{
    public Guid Id { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public int RequiredPriorityRating { get; private set; }
    public Guid? AssignedEmployeeId { get; private set; }   
    public ShiftStatus Status { get; private set; }

    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    private Shift() { } // For EF Core

    public Shift(DateTime startTime, DateTime endTime, int requiredPriorityRating)
    {
        if (startTime >= endTime)
            throw new ArgumentException("Start time must be before end time");

        if (requiredPriorityRating < 1 || requiredPriorityRating > 5)
            throw new ArgumentException("Required priority rating must be between 1 and 5");

        Id = Guid.NewGuid();
        StartTime = startTime;
        EndTime = endTime;
        RequiredPriorityRating = requiredPriorityRating;
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