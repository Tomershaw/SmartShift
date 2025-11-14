using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;
using SmartShift.Domain.Features.ShiftRegistrations;
using SmartShift.Domain.Features.Employees;
using System.Linq;

namespace SmartShift.Application.Features.Scheduling.DayRegistrants;

public sealed class GetDayRegistrantsQueryHandler
  : IRequestHandler<GetDayRegistrantsQuery, IReadOnlyList<DayRegistrantNameDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IShiftRepository _shiftRepository;

    public GetDayRegistrantsQueryHandler(
        ICurrentUserService currentUser,
        IShiftRepository shiftRepository)
    {
        _currentUser = currentUser;
        _shiftRepository = shiftRepository;
    }

    public async Task<IReadOnlyList<DayRegistrantNameDto>> Handle(GetDayRegistrantsQuery request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        // גבולות היום המקומיים (IL) ב-UTC: [start, end)
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");
        DateTime ToUtc(DateOnly d) => TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), tz);
        var startUtc = ToUtc(request.DayLocal);
        var endUtcExclusive = ToUtc(request.DayLocal.AddDays(1));

        // שולפים את כל המשמרות של אותו יום כולל ההרשמות והעובדים
        var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(
            startUtc, endUtcExclusive, tenantId, ct);

        // פותחים להרשמות, מסננים סטטוס אם התבקש, וממפים לשמות
        var regs = shifts
            .SelectMany(s => s.ShiftRegistrations ?? Enumerable.Empty<ShiftRegistration>())
            .Where(r => r.Employee != null);

        if (request.Status is ShiftRegistrationStatus filter)
            regs = regs.Where(r => r.Status == filter);

        var ordered = regs
            .OrderBy(r => r.Employee!.LastName ?? string.Empty)
            .ThenBy(r => r.Employee!.FirstName ?? string.Empty)
            .Skip(Math.Max(0, request.Skip))
            .Take(Math.Max(0, request.Take));

        var list = ordered.Select(r => new DayRegistrantNameDto(
            FirstName: r.Employee!.FirstName ?? string.Empty,
            LastName: r.Employee!.LastName ?? string.Empty,
            Status: r.Status,
            ShiftArrivalType: r.ShiftArrivalType  // ✅ זה מחזיר את מה שהעובד בחר בהרשמה הספציפית!
        )).ToList();

        return list;
    }
}
