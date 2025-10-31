using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesHandler
    : IRequestHandler<ApproveShiftEmployeesCommand, ApproveShiftEmployeesResult>
{
    private readonly IShiftRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public ApproveShiftEmployeesHandler(IShiftRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ApproveShiftEmployeesResult> Handle(ApproveShiftEmployeesCommand req, CancellationToken ct)
    {
        var p = req.Payload;

        // שינוי 1 - איחוד ובדיקה מוקדמת
        var employeeIds = p.EmployeeIds?.Distinct().ToList() ?? new List<Guid>();
        if (employeeIds.Count == 0)
        {
            return new ApproveShiftEmployeesResult
            {
                Success = false,
                ApprovedCount = 0,
                ApprovedIds = new List<Guid>(),
                RequestedButNotPending = new List<Guid>(),
                StillPendingAfterApprove = new List<Guid>(),
                Message = "EmployeeIds required"
            };
        }

        var tenantId = _currentUser.GetTenantId();
        var reviewerId = Guid.Parse(_currentUser.GetUserId());

        // שינוי 2 - שימוש ב־req.ShiftId במקום p.ShiftId
        var pendings = await _repo.GetPendingRegistrationsAsync(tenantId, req.ShiftId, ct);
        var byEmp = pendings.ToDictionary(r => r.EmployeeId, r => r);
        var pendingIds = pendings.Select(r => r.EmployeeId).ToHashSet();

        var requestedButNotPending = employeeIds.Where(id => !pendingIds.Contains(id)).ToList();

        var approvedIds = new List<Guid>();
        foreach (var empId in employeeIds)
        {
            if (!byEmp.TryGetValue(empId, out var reg)) continue;

            var ok = await _repo.ApproveShiftRegistrationAsync(reg.Id, reviewerId, comment: null, tenantId, ct);
            if (ok) approvedIds.Add(empId);
        }

        // שינוי 3 - גם כאן req.ShiftId
        var pendingsAfter = await _repo.GetPendingRegistrationsAsync(tenantId, req.ShiftId, ct);
        var pendingsAfterSet = pendingsAfter.Select(r => r.EmployeeId).ToHashSet();

        var stillPendingAfterApprove = employeeIds.Where(id => pendingsAfterSet.Contains(id)).ToList();

        var approvedActually = employeeIds.Count
                               - stillPendingAfterApprove.Count
                               - requestedButNotPending.Count;

        return new ApproveShiftEmployeesResult
        {
            Success = stillPendingAfterApprove.Count == 0,
            ApprovedCount = approvedActually,
            ApprovedIds = approvedIds,
            RequestedButNotPending = requestedButNotPending,
            StillPendingAfterApprove = stillPendingAfterApprove,
            Message = stillPendingAfterApprove.Count == 0
                ? null
                : "חלק מהעובדים לא אושרו בפועל (ראה StillPendingAfterApprove)."
        };
    }
}

