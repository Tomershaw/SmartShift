using System;

namespace SmartShift.Domain.Features.Employees;

public class Employee
{
    public Guid Id { get; private set; }
    public string?  FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public int PriorityRating { get; private set; }

    public string Name => $"{FirstName} {LastName}";

    // EF Core requires a parameterless constructor
    private Employee() { }

    public Employee(string firstName, string lastName, string email, string phoneNumber, int priorityRating = 0)
    {
        Id = Guid.NewGuid();
        FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : throw new ArgumentException("First name is required.");
        LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : throw new ArgumentException("Last name is required.");
        Email = !string.IsNullOrWhiteSpace(email) ? email : throw new ArgumentException("Email is required.");
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : throw new ArgumentException("Phone number is required.");
        PriorityRating = priorityRating;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string firstName, string lastName, string email, string phoneNumber, int priorityRating)
    {
        FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : throw new ArgumentException("First name is required.");
        LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : throw new ArgumentException("Last name is required.");
        Email = !string.IsNullOrWhiteSpace(email) ? email : throw new ArgumentException("Email is required.");
        PhoneNumber = !string.IsNullOrWhiteSpace(phoneNumber) ? phoneNumber : throw new ArgumentException("Phone number is required.");
        PriorityRating = priorityRating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePriorityRating(int newRating)
    {
        PriorityRating = newRating;
        UpdatedAt = DateTime.UtcNow;
    }
}
