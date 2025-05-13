using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedEmployeesAsync(ApplicationDbContext context)
    {
        try
        {
            if (await context.Employees.AnyAsync())
                return; // Database already seeded

            var employees = new List<Employee>
            {
                new Employee("John", "Doe", "john.doe@smartshift.com", "555-0101", 3),
                new Employee("Jane", "Smith", "jane.smith@smartshift.com", "555-0102", 2),
                new Employee("Michael", "Johnson", "michael.johnson@smartshift.com", "555-0103", 1)
            };

            await context.Employees.AddRangeAsync(employees);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding employees: {ex.Message}");
        }
    }

    public static async Task SeedShiftsAsync(ApplicationDbContext context)
    {
        try
        {
            if (await context.Shifts.AnyAsync())
                return; // Shifts already seeded

            var employees = await context.Employees.ToListAsync();

            var shifts = new List<Shift>
            {
                new Shift(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(8), 3),
                new Shift(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(8), 2),
                new Shift(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(8), 1)
            };

            shifts[0].AssignEmployee(employees[0].Id);
            shifts[1].AssignEmployee(employees[1].Id);
            shifts[2].AssignEmployee(employees[2].Id);

            await context.Shifts.AddRangeAsync(shifts);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding shifts: {ex.Message}");
        }
    }
}