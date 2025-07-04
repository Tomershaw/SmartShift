using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using System.ComponentModel.DataAnnotations;
namespace SmartShift.Domain.Features.ShiftRegistrations; 

public class ShiftRegistration
{

    public Guid Id { get; private set; }
    public Guid ShiftId { get; set; }
    public Guid EmployeeId { get;  set;}

    public DateTime RegisteredAt { get; private set; }
    public Guid TenantId { get; set; }

    public ShiftRegistrationStatus Status { get; private set; }

    // הוסף את אלה כאן:
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? ReviewComment { get; private set; }

   
    public required Shift Shift { get; set; }
    
    public required Employee Employee { get; set; }
    
    public required Tenant Tenant { get; set; }

    public ShiftRegistration() { } // For EF Core

    public ShiftRegistration(Guid shiftId, Guid employeeId, Guid tenantId)
    {
        Id = Guid.NewGuid();
        ShiftId = shiftId;
        EmployeeId = employeeId;
        TenantId = tenantId;
        RegisteredAt = DateTime.UtcNow;
        Status = ShiftRegistrationStatus.Pending;
    }

    // הוסף את המתודות האלה:
    public void Approve(Guid reviewedBy, string? comment = null)
    {
        Status = ShiftRegistrationStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        ReviewComment = comment;
    }

    public void Reject(Guid reviewedBy, string? comment = null)
    {
        Status = ShiftRegistrationStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewedBy;
        ReviewComment = comment;
    }

    public void Cancel()
    {
        Status = ShiftRegistrationStatus.Cancelled;
    }

    // Enum כבר יש לך

}
