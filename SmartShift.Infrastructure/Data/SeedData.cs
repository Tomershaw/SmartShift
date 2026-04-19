using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using System.Reflection;

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

    public static async Task SeedEmployeesAsync(ApplicationDbContext context, IServiceProvider serviceProvider, Guid tenantId)
    {
        try
        {
            if (await context.Employees.AnyAsync())
                return;

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            Console.WriteLine("🚀 Creating 20 unique users and employees using Reflection...");

            var firstNames = new[] { "איתי", "נועה", "יוסי", "מאיה", "דורון", "עמית", "ליאור", "טל", "רוני", "שי", "אביב", "מיכל", "דניאל", "עדי", "יובל" };
            var lastNames = new[] { "כהן", "לוי", "מזרחי", "פרץ", "ביטון", "דהן", "אברהם", "פרידמן", "מלכה", "אזולאי", "חדד", "גבאי", "סוויסה" };

            var faker = new Faker();

            for (int i = 0; i < 20; i++)
            {
                var firstName = faker.PickRandom(firstNames);
                var lastName = faker.PickRandom(lastNames);
                var email = faker.Internet.Email(firstName, lastName);
                var userName = $"user_{faker.Random.Number(1000, 9999)}";

                // 1. יצירת המשתמש
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userName,
                    Email = email,
                    FullName = $"{firstName} {lastName}",
                    TenantId = tenantId,
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, "User123!");

                if (result.Succeeded)
                {
                    // 2. יצירת העובד דרך הבנאי התקני שלך
                    var employee = new Employee(
                        firstName,
                        lastName,
                        email,
                        faker.Phone.PhoneNumber("054#######"),
                        faker.Random.Int(1, 5) // SkillLevel
                    );

                    // 3. הזרקת שאר הנתונים (עקיפת ה-private set) באמצעות Reflection
                    SetPrivatePropertyValue(employee, "Id", Guid.NewGuid());
                    SetPrivatePropertyValue(employee, "TenantId", tenantId);
                    SetPrivatePropertyValue(employee, "UserId", user.Id);
                    SetPrivatePropertyValue(employee, "Gender", faker.PickRandom("Male", "Female"));
                    SetPrivatePropertyValue(employee, "PriorityRating", faker.Random.Int(0, 10));
                    SetPrivatePropertyValue(employee, "MaxShiftsPerWeek", faker.Random.Int(3, 5));
                    SetPrivatePropertyValue(employee, "AdminNotes", faker.PickRandom(new[] {
                    "עובד מצטיין, תמיד מגדיל ראש",
                    "נוטה לאחר, דורש השגחה",
                    "מקצוען אמיתי, מתאים לניהול",
                    "ראש קטן, עושה מינימום",
                    "בעיות משמעת לפעמים",
                    "עובד חדש ויציב"
                }));

                    await context.Employees.AddAsync(employee);
                    await userManager.AddToRoleAsync(user, "Employee");
                }
            }

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Successfully created 20 unique Users and Employees!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding: {ex.Message}");
        }
    }

    // פונקציית עזר קטנה ששמים באותו קובץ (SeedData.cs)
    // היא יודעת לקחת אובייקט ולהזריק כוחנית ערך לתוך שדה, גם אם הוא private set
    private static void SetPrivatePropertyValue<T>(T obj, string propertyName, object value)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(obj, value, null);
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
                new Shift(
                    startTime: DateTime.UtcNow.AddDays(1),
                    minimumEarlyEmployees:4,
                    requiredEmployeeCount: 3,
                    minimumEmployeeCount: 2,
                    skillLevelRequired: 2,
                    description: "Morning shift - warehouse"
                )
                {
                    TenantId = tenantId
                },
                new Shift(
                    startTime: DateTime.UtcNow.AddDays(2),
                     minimumEarlyEmployees:4,
                    requiredEmployeeCount: 2,
                    minimumEmployeeCount: 1,
                    skillLevelRequired: 3,
                    description: "Evening shift - packaging"
                )
                {
                    TenantId = tenantId
                },
                new Shift(
                    startTime: DateTime.UtcNow.AddDays(3),
                     minimumEarlyEmployees:4,
                    requiredEmployeeCount: 1,
                    minimumEmployeeCount: 1,
                    skillLevelRequired: 1,
                    description: "Night shift - support"
                )
                {
                    TenantId = tenantId
                }
            };

            // שיבוץ עובדים (אופציונלי)
            if (employees.Count >= 3)
            {
                shifts[0].AssignEmployee(employees[0].Id);
                shifts[1].AssignEmployee(employees[1].Id);
                shifts[2].AssignEmployee(employees[2].Id);
            }

            await context.Shifts.AddRangeAsync(shifts);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Shifts seeded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding shifts: {ex.Message}");
        }
    }



    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, Guid tenantId)
    {
        try
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // ההגדרות הקבועות שלנו
            var targetUserName = "simon1";
            var targetEmail = "SimonHamelech@gmail.com";
            var targetId = "c0d7a0b3-9580-4ad8-ab65-7fbd4fc31ff8"; // ה-ID שאתה חייב

            // 1. חיפוש המשתמש לפי שם (כולל מחוקים)
            var existingUser = await userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UserName == targetUserName);

            // 2. בדיקת התנגשות: האם המשתמש קיים אבל עם ID לא נכון?
            if (existingUser != null && existingUser.Id.ToString().ToLower() != targetId.ToLower())
            {
                Console.WriteLine($"⚠️ User '{targetUserName}' exists but with WRONG ID ({existingUser.Id}). Deleting to recreate with correct ID...");

                // מחיקה כפויה של המשתמש הישן כדי לפנות את השם
                // קודם מנקים תפקידים כדי למנוע שגיאות Foreign Key
                var roles = await userManager.GetRolesAsync(existingUser);
                if (roles.Count > 0)
                {
                    await userManager.RemoveFromRolesAsync(existingUser, roles);
                }

                await userManager.DeleteAsync(existingUser);
                existingUser = null; // מאפסים כדי שייכנס ללוגיקת היצירה למטה
                Console.WriteLine("🗑️ Wrong user deleted.");
            }

            // 3. בדיקה נוספת: האם ה-ID תפוס ע"י מישהו אחר (שם משתמש אחר)?
            if (existingUser == null)
            {
                var userWithTargetId = await userManager.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == targetId);

                if (userWithTargetId != null)
                {
                    // מקרה נדיר: ה-ID תפוס אבל השם משתמש שונה. מוחקים גם אותו.
                    Console.WriteLine($"⚠️ Target ID is taken by '{userWithTargetId.UserName}'. Deleting...");
                    await userManager.DeleteAsync(userWithTargetId);
                }
            }

            // 4. יצירה מחדש (אם המשתמש לא קיים או שנמחק בשלבים הקודמים)
            if (existingUser == null)
            {
                Console.WriteLine("Creating new admin user with FORCED ID...");

                var newAdmin = new ApplicationUser
                {
                    Id = targetId, // 👈 הקביעה הקריטית של ה-ID
                    UserName = targetUserName,
                    Email = targetEmail,
                    EmailConfirmed = true,
                    FullName = "Simon Shaw",
                    TenantId = tenantId,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(newAdmin, "Change12!");
                if (!result.Succeeded)
                {
                    Console.WriteLine("❌ Failed to create user:");
                    foreach (var err in result.Errors) Console.WriteLine($"   {err.Description}");
                    return;
                }

                existingUser = newAdmin; // עדכון המשתנה להמשך
                Console.WriteLine("✅ User created successfully.");
            }
            else
            {
                // המשתמש קיים ועם ה-ID הנכון
                Console.WriteLine("✅ User exists with correct ID. Verifying status...");
                bool needUpdate = false;

                if (!existingUser.IsActive)
                {
                    existingUser.IsActive = true;
                    existingUser.DeletedAt = null;
                    needUpdate = true;
                    Console.WriteLine("🔄 Reactivating user...");
                }

                if (existingUser.Email != targetEmail)
                {
                    existingUser.Email = targetEmail;
                    needUpdate = true;
                }

                if (needUpdate) await userManager.UpdateAsync(existingUser);
            }

            // 5. וידוא תפקיד Admin (תמיד רץ בסוף)
            if (!await userManager.IsInRoleAsync(existingUser, "Admin"))
            {
                await userManager.AddToRoleAsync(existingUser, "Admin");
                Console.WriteLine("✅ Role 'Admin' assigned.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding admin: {ex.Message}");
        }
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
