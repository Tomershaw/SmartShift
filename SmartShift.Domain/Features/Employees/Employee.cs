using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Scheduling;
using SmartShift.Domain.Features.ShiftRegistrations;
using System;
using System.Collections.Generic;

namespace SmartShift.Domain.Features.Employees;

public class Employee
{
    public Guid Id { get; private set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; } // ניווט ל-User

    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public int PriorityRating { get; private set; }

    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public ICollection<ShiftRegistration> ShiftRegistrations { get; set; } = new List<ShiftRegistration>();
    
    public string Name => $"{FirstName} {LastName}";

    // תוספות חדשות
    public int SkillLevel { get; private set; } // רמת מיומנות
    public List<string> WorkTypes { get; private set; } = new List<string>(); // סוגי עבודה
    public int MaxShiftsPerWeek { get; private set; } // מספר משמרות מקסימלי בשבוע
    public string Notes { get; private set; } = string.Empty; // הערות על העובד

    // EF Core requires a parameterless constructor
    private Employee() { }

    public Employee(string firstName, string lastName, string email, string phoneNumber, int priorityRating = 0, int skillLevel = 1, List<string>? workTypes = null, int maxShiftsPerWeek = 0, string notes = "")
    {
        Id = Guid.NewGuid();
        FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : throw new ArgumentException("First name is required.");
        LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : throw new ArgumentException("Last name is required.");
        Email = !string.IsNullOrWhiteSpace(email) ? email : throw new ArgumentException("Email is required.");
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : throw new ArgumentException("Phone number is required.");
        PriorityRating = priorityRating;
        CreatedAt = DateTime.UtcNow;

        // תוספות חדשות
        SkillLevel = skillLevel;
        WorkTypes = workTypes ?? new List<string>();
        MaxShiftsPerWeek = maxShiftsPerWeek;
        Notes = notes;
    }

    public void Update(string firstName, string lastName, string email, string phoneNumber, int priorityRating, int skillLevel, List<string> workTypes, int maxShiftsPerWeek, string notes)
    {
        FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : throw new ArgumentException("First name is required.");
        LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : throw new ArgumentException("Last name is required.");
        Email = !string.IsNullOrWhiteSpace(email) ? email : throw new ArgumentException("Email is required.");
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : throw new ArgumentException("Phone number is required.");
        PriorityRating = priorityRating;
        UpdatedAt = DateTime.UtcNow;

        // עדכון תוספות חדשות
        SkillLevel = skillLevel;
        WorkTypes = workTypes ?? new List<string>();
        MaxShiftsPerWeek = maxShiftsPerWeek;
        Notes = notes;
    }

    public void UpdatePriorityRating(int newRating)
    {
        PriorityRating = newRating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSkillLevel(int newSkillLevel)
    {
        if (newSkillLevel < 1 || newSkillLevel > 5)
            throw new ArgumentException("Skill level must be between 1 and 5");

        SkillLevel = newSkillLevel;
    }

    public void UpdateMaxShiftsPerWeek(int newMaxShifts)
    {
        if (newMaxShifts < 0)
            throw new ArgumentException("Max shifts per week must be non-negative");

        MaxShiftsPerWeek = newMaxShifts;
    }

    public void AddWorkType(string workType)
    {
        if (string.IsNullOrWhiteSpace(workType))
            throw new ArgumentException("Work type cannot be null or empty");

        WorkTypes.Add(workType);
    }

    public void UpdateNotes(string newNotes)
    {
        Notes = newNotes;
    }
}
