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
    public int MaxShiftsPerWeek { get; private set; } // מספר משמרות מקסימלי בשבוע
    public string AdminNotes { get; private set; } = string.Empty; // הערות על העובד
    public string EmployeeNotes { get; private set; } = string.Empty;

    // EF Core requires a parameterless constructor
    private Employee() { }

    public Employee(string firstName, string lastName, string email, string phoneNumber,
        int priorityRating = 0, int skillLevel = 1,
        int maxShiftsPerWeek = 0, string adminNotes = "", string employeeNotes = "")
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
        MaxShiftsPerWeek = maxShiftsPerWeek;
    }

    private void Update(string firstName, string lastName, string email, string phoneNumber,
        int priorityRating, int skillLevel, 
        int maxShiftsPerWeek)
    {
        FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : throw new ArgumentException("First name is required.");
        LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : throw new ArgumentException("Last name is required.");
        Email = !string.IsNullOrWhiteSpace(email) ? email : throw new ArgumentException("Email is required.");
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : throw new ArgumentException("Phone number is required.");
        PriorityRating = priorityRating;
        UpdatedAt = DateTime.UtcNow;

        // עדכון תוספות חדשות
        SkillLevel = skillLevel;
        MaxShiftsPerWeek = maxShiftsPerWeek;

    }
    public void AdminUpdate(string firstName, string lastName, string email, string phoneNumber,
                            int priorityRating, int skillLevel, 
                            int maxShiftsPerWeek, string adminNotes)
    {
        Update(firstName, lastName, email, phoneNumber, priorityRating, skillLevel,  maxShiftsPerWeek);
        AdminNotes = adminNotes;
    }
    public void EployeeUpdate(string firstName, string lastName, string email, string phoneNumber,
                            int priorityRating, int skillLevel, 
                            int maxShiftsPerWeek, string employeeNotes)
    {
        Update(firstName, lastName, email, phoneNumber, priorityRating, skillLevel,  maxShiftsPerWeek);
        EmployeeNotes = employeeNotes;
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
    public void UpdateNotes(string newNotes)
    {
        AdminNotes = newNotes;
    }
}
