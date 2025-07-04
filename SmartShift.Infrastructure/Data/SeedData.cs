using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedTenantAsync(ApplicationDbContext context)
    {
        try
        {
            if (await context.Tenants.AnyAsync())
                return;

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "אריא"
            };

            await context.Tenants.AddAsync(tenant);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Tenant 'אריא' seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding tenant: {ex.Message}");
        }
    }

    public static async Task SeedEmployeesAsync(ApplicationDbContext context, Guid tenantId)
    {
        try
        {
            if (await context.Employees.AnyAsync())
                return;

            var employees = new List<Employee>
            {
                new Employee("John", "Doe", "john.doe@smartshift.com", "555-0101", 3) { TenantId = tenantId },
                new Employee("Jane", "Smith", "jane.smith@smartshift.com", "555-0102", 2) { TenantId = tenantId },
                new Employee("Michael", "Johnson", "michael.johnson@smartshift.com", "555-0103", 1) { TenantId = tenantId }
            };

            await context.Employees.AddRangeAsync(employees);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Employees seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding employees: {ex.Message}");
        }
    }

    public static async Task SeedShiftsAsync(ApplicationDbContext context, Guid tenantId)
    {
        try
        {
            if (await context.Shifts.AnyAsync())
                return;

            var employees = await context.Employees.ToListAsync();

            var shifts = new List<Shift>
        {
            new Shift(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(8), 3)
            {
                TenantId = tenantId
            },
            new Shift(DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(8), 2)
            {
                TenantId = tenantId
            },
            new Shift(DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(8), 1)
            {
                TenantId = tenantId
            }
        };

            // שיבוץ עובדים (אופציונלי אם אתה רוצה להדגים)
            shifts[0].AssignEmployee(employees[0].Id);
            shifts[1].AssignEmployee(employees[1].Id);
            shifts[2].AssignEmployee(employees[2].Id);

            await context.Shifts.AddRangeAsync(shifts);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Shifts seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding shifts: {ex.Message}");
        }
    }


    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, Guid  tenantId)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var adminEmail = "simon1@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "simon1",
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Simon Shaw",
                TenantId = tenantId
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (!result.Succeeded)
            {
                Console.WriteLine("❌ Failed to create admin user:");
                foreach (var error in result.Errors)
                    Console.WriteLine($"   {error.Description}");
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        Console.WriteLine("✅ Admin user seeded successfully.");
    }


    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { "Admin", "Employee" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            Console.WriteLine("✅ Roles seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding roles: {ex.Message}");
        }
    }
}
