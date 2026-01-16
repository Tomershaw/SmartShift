using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartShift.Domain.Data;

public class ApplicationUser : IdentityUser
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    // 🟢 התוספת שלך - הכי מדויק שיש
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? DeletedAt { get; set; }
}