using SmartShift.Domain.Data;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.Scheduling;
using System.ComponentModel.DataAnnotations;
namespace SmartShift.Domain.Features.ShiftRegistrations;

public class ShiftRegistration
{

    public Guid Id { get; private set; }
    public Guid ShiftId { get; set; }
    public Guid EmployeeId { get; set; }

    public DateTime RegisteredAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid TenantId { get; set; }

    public ShiftRegistrationStatus Status { get; private set; }

    // הוסף את אלה כאן:
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? ReviewComment { get; private set; }

    public EmployeeShiftAvailability ShiftArrivalType { get; private set; } // זמינות למשמרות - גנרי

    public required Shift Shift { get; set; }

    public required Employee Employee { get; set; }

    public required Tenant Tenant { get; set; }

    public ShiftRegistration() { } // For EF Core

    public ShiftRegistration(Guid shiftId, Guid employeeId, Guid tenantId, EmployeeShiftAvailability shiftArrivalType)
    {
        Id = Guid.NewGuid();
        ShiftId = shiftId;
        EmployeeId = employeeId;
        TenantId = tenantId;
        RegisteredAt = DateTime.UtcNow;
        Status = ShiftRegistrationStatus.Pending;
        ShiftArrivalType = shiftArrivalType; // הוספת אתחול לשדה החדש
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

    //public void UpdateShiftAvailability(EmployeeShiftAvailability availability)
    //{
    //    ShiftAvailability = availability;
    //    UpdatedAt = DateTime.UtcNow;
    //}

    /// <summary>
    /// בדיקה האם עובד זמין לסוג משמרת מסוים
    /// </summary>
    //public bool IsAvailableForShiftType(EmployeeShiftAvailability requiredAvailability)
    //{
    //    // אם דורשים Regular - כולם יכולים
    //    if (requiredAvailability == EmployeeShiftAvailability.Regular)
    //    {
    //        return true;
    //    }

    //    // אם דורשים Early - רק מי שבחר Early
    //    if (requiredAvailability == EmployeeShiftAvailability.Early)
    //    {
    //        return ShiftAvailability == EmployeeShiftAvailability.Early;
    //    }

    //    return false;
    //}


    public void UpdateShiftAvailability(EmployeeShiftAvailability availability)
    {

        ShiftArrivalType = availability;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsAvailableForShiftType(EmployeeShiftAvailability requiredAvailability)
    {
        if (ShiftArrivalType == null)
        {
            throw new InvalidOperationException("Shift availability is not set.");
        }

        if (requiredAvailability == EmployeeShiftAvailability.Regular)
        {
            return true;
        }

        if (requiredAvailability == EmployeeShiftAvailability.Early)
        {
            return ShiftArrivalType == EmployeeShiftAvailability.Early;
        }

        return false;
    }

    public void Cancel()
    {
        Status = ShiftRegistrationStatus.Cancelled;
    }
}
