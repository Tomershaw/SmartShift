using MediatR;

namespace SmartShift.Application.Features.ProcessShifts;

public sealed class ProcessShiftsCommand : IRequest<ProcessShiftsResult>
{
    public Guid TenantId { get; set; }

    // במקום לקבל DateTime כאן, נקבל מחרוזות ונטפל ב-Handler
    public string? StartString { get; set; }
    public string? EndString { get; set; }

    // לוגיקה פנימית ב-Handler תחשב Start/End בפועל
}