    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using SmartShift.Domain.Data;
    using SmartShift.Domain.Features.Employees;
    using SmartShift.Domain.Features.RefreshTokens;
    using SmartShift.Domain.Features.Scheduling;
    using SmartShift.Domain.Features.ShiftRegistrations;

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
        public DbSet<ShiftRegistration> ShiftRegistrations { get; set; } // חדש!

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Shift>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartTime).IsRequired();
                
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
                .IsRequired();

            modelBuilder.Entity<Employee>()
              .HasOne(e => e.User)
              .WithOne()
              .HasForeignKey<Employee>(e => e.UserId)
              .IsRequired(false);

            modelBuilder.Entity<Employee>()
               .HasIndex(e => e.UserId)
               .IsUnique();

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Tenant)
                .WithMany(t => t.Shifts)
                .HasForeignKey(s => s.TenantId)
                .IsRequired();


        modelBuilder.Entity<ShiftRegistration>(entity =>
        {
            entity.HasKey(sr => sr.Id);

            entity.HasOne(sr => sr.Shift)
                  .WithMany(s => s.ShiftRegistrations)
                  .HasForeignKey(sr => sr.ShiftId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(sr => sr.Employee)
                  .WithMany(e => e.ShiftRegistrations)
                  .HasForeignKey(sr => sr.EmployeeId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(sr => sr.Tenant)
                  .WithMany()
                  .HasForeignKey(sr => sr.TenantId)
                  .OnDelete(DeleteBehavior.NoAction);

            // אינדקס ייחודי - עובד יכול להירשם פעם אחת למשמרת
            entity.HasIndex(sr => new { sr.TenantId, sr.EmployeeId, sr.ShiftId })
                  .IsUnique()
                  .HasDatabaseName("UX_ShiftRegistration_Active")
                  .HasFilter("[Status] IN (0,1)");
        });
    }
    }

