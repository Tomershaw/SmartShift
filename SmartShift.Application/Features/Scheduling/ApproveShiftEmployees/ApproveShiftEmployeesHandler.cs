using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.ApproveShiftEmployees;

public sealed class ApproveShiftEmployeesHandler
    : IRequestHandler<ApproveShiftEmployeesCommand, ApproveShiftEmployeesResult>
{
    private readonly IShiftRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public ApproveShiftEmployeesHandler(
        IShiftRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ApproveShiftEmployeesResult> Handle(
        ApproveShiftEmployeesCommand req,
        CancellationToken ct)
    {
        var p = req.Payload;

        if (p.EmployeeIds == null || p.EmployeeIds.Count == 0)
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

        // כל מי ש-Pending במשמרת
        var pendings = await _repo.GetPendingRegistrationsAsync(tenantId, p.ShiftId, ct);
        var byEmp = pendings.ToDictionary(r => r.EmployeeId, r => r);
        var requestedSet = p.EmployeeIds.Distinct().ToHashSet();
        var pendingIds = pendings.Select(r => r.EmployeeId).ToHashSet();

        // התבקשו אבל בכלל לא Pending כרגע (או שאינם רשומים למשמרת הזו)
        var requestedButNotPending = requestedSet.Except(pendingIds).ToList();

        // מאשרים רק את מי שב-Pending + נמצא ב-Request
        var approvedIds = new List<Guid>();
        foreach (var empId in requestedSet)
        {
            if (!byEmp.TryGetValue(empId, out var reg))
                continue;

            var ok = await _repo.ApproveShiftRegistrationAsync(
                reg.Id,
                reviewerId,
                comment: null,
                tenantId,
                ct);
                
            if (ok) approvedIds.Add(empId);
        }

        // בדיקת מצב אחרי האישור
        var pendingsAfter = await _repo.GetPendingRegistrationsAsync(tenantId, p.ShiftId, ct);
        var pendingsAfterSet = pendingsAfter.Select(r => r.EmployeeId).ToHashSet();

        var stillPendingAfterApprove = requestedSet.Intersect(pendingsAfterSet).ToList();

        var approvedActually = requestedSet.Count
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
