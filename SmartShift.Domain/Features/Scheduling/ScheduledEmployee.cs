using System;
using System.Collections.Generic;

namespace SmartShift.Domain.Features.Scheduling;

public class ScheduledEmployee

{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required int PriorityRating { get; set; }
    public required List<Availability> Availabilities { get; init; }
    public required List<Shift> AssignedShifts { get; init; }

    private ScheduledEmployee() { } // For EF Core

    public ScheduledEmployee(string name, string email, string phoneNumber)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;
        PriorityRating = 1; // Default rating
        Availabilities = new List<Availability>();
        AssignedShifts = new List<Shift>();
    }

    public void UpdatePriorityRating(int newRating)
    {
        if (newRating < 1 || newRating > 5)
            throw new ArgumentException("Priority rating must be between 1 and 5");
        
        PriorityRating = newRating;
    }

    public void AddAvailability(Availability availability)
    {
        Availabilities.Add(availability);
    }

    public bool IsAvailableForShift(Shift shift)
    {
        return Availabilities.Any(a => a.CoversShift(shift));
    }
} 