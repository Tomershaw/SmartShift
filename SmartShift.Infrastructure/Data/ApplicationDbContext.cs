using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.RefreshTokens;
using SmartShift.Domain.Features.Scheduling;

namespace SmartShift.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Shift> Shifts { get; set; }

    public DbSet<Tenant> Tenants { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime).IsRequired();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired();
            entity.Property(e => e.LastName).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.PhoneNumber).IsRequired();
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .IsRequired(); // ⬅️ הכי חשוב כדי למנוע קריסה


        modelBuilder.Entity<Employee>()
        .HasOne(e => e.Tenant)
        .WithMany(t => t.Employees) // אם יש לך ICollection<Employee> ב-Tenant
        .HasForeignKey(e => e.TenantId)
        .IsRequired();

        modelBuilder.Entity<Shift>()
            .HasOne(s => s.Tenant)
            .WithMany(t => t.Shifts) // אם יש ICollection<Shift> ב-Tenant
            .HasForeignKey(s => s.TenantId)
            .IsRequired();
    }
}
