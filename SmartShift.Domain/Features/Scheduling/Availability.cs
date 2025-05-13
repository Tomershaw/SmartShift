using System;

namespace SmartShift.Domain.Features.Scheduling;

public class Availability
{
    public Guid Id { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public bool IsRecurring { get; private set; }

    private Availability() { } // For EF Core

    public Availability(DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, bool isRecurring = true)
    {
        if (startTime >= endTime)
            throw new ArgumentException("Start time must be before end time");

        Id = Guid.NewGuid();
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsRecurring = isRecurring;
    }

    public bool CoversShift(Shift shift)
    {
        if (!IsRecurring)
            return false;

        return shift.StartTime.DayOfWeek == DayOfWeek &&
               shift.StartTime.TimeOfDay >= StartTime &&
               shift.EndTime.TimeOfDay <= EndTime;
    }
} 