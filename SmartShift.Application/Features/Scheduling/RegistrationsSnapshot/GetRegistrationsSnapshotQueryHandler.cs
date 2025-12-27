using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;
using SmartShift.Application.Features.Scheduling.RegistrationsSnapshot;
using SmartShift.Domain.Features.ShiftRegistrations;

namespace SmartShift.Application.Features.Scheduling.RegistrationsSnapshot;

public sealed class GetRegistrationsSnapshotQueryHandler
    : IRequestHandler<GetRegistrationsSnapshotQuery, IReadOnlyList<DayRegistrationSnapshotDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IShiftRepository _shiftRepository;

    public GetRegistrationsSnapshotQueryHandler(
        ICurrentUserService currentUser,
        IShiftRepository shiftRepository)
    {
        _currentUser = currentUser;
        _shiftRepository = shiftRepository;
    }

    public async Task<IReadOnlyList<DayRegistrationSnapshotDto>> Handle(
        GetRegistrationsSnapshotQuery request,
        CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        // ממירים טווח ימים מקומי לגבולות UTC לשאילתת DB
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

        // DateTime ToUtc(DateOnly d) =>
        //    TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), tz);

        // startUtc כולל, endUtc בלעדי
        //  var startUtc = ToUtc(request.FromLocal);
        //   var endUtcExclusive = ToUtc(request.ToLocal.AddDays(1));

        // המרה מ-DateOnly ל-DateTimeOffset
        var startLocal = new DateTime(request.FromLocal.Year, request.FromLocal.Month, request.FromLocal.Day, 0, 0, 0);
        var endLocal = new DateTime(request.ToLocal.Year, request.ToLocal.Month, request.ToLocal.Day, 0, 0, 0).AddDays(1);

        var startUtc = new DateTimeOffset(startLocal, tz.GetUtcOffset(startLocal)).ToUniversalTime();
        var endUtcExclusive = new DateTimeOffset(endLocal, tz.GetUtcOffset(endLocal)).ToUniversalTime();

        // שולפים משמרות בטווח כולל הרשמות
        var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(
            startUtc,
            endUtcExclusive,
            tenantId,
            ct);

        // מקבצים לפי תאריך מקומי בישראל
        var grouped = shifts
            .GroupBy(s =>
            {
                var local = TimeZoneInfo.ConvertTime(s.StartTime, tz);
                return DateOnly.FromDateTime(local.Date);
            })
            .OrderBy(g => g.Key);

        var list = new List<DayRegistrationSnapshotDto>();

        foreach (var g in grouped)
        {
            var regs = g.SelectMany(s => s.ShiftRegistrations ?? Enumerable.Empty<Domain.Features.ShiftRegistrations.ShiftRegistration>())
                        .ToList();

            int Count(ShiftRegistrationStatus st) => regs.Count(r => r.Status == st);

            var dto = new DayRegistrationSnapshotDto(
                Date: g.Key, // תאריך מקומי Asia/Jerusalem
                RequiredCount: g.Sum(s => s.RequiredEmployeeCount),
                RegisteredCount: regs.Count,
                PendingCount: Count(ShiftRegistrationStatus.Pending),
                ApprovedCount: Count(ShiftRegistrationStatus.Approved),
                RejectedCount: Count(ShiftRegistrationStatus.Rejected),
                CancelledCount: Count(ShiftRegistrationStatus.Cancelled)
            );

            list.Add(dto);
        }

        // משלימים ימים חסרים בטווח עם אפסים כדי לקבל רצף מלא
        var filled = new List<DayRegistrationSnapshotDto>();
        for (var d = request.FromLocal; d <= request.ToLocal; d = d.AddDays(1))
        {
            var row = list.FirstOrDefault(x => x.Date == d)
                      ?? new DayRegistrationSnapshotDto(d, 0, 0, 0, 0, 0, 0);
            filled.Add(row);
        }

        return filled;
    }
}
