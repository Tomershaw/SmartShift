using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Features.Scheduling.Repositories;

namespace SmartShift.Application.Features.Scheduling.Queries.GetShifts;

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, IEnumerable<ShiftDto>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetShiftsQueryHandler(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService)
    {
        _shiftRepository = shiftRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<ShiftDto>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.GetTenantId();

        var startDate = DateTime.Parse(request.StartDate);
        var endDate = DateTime.Parse(request.EndDate);

        var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(startDate, tenantId, cancellationToken);

        return shifts.Select(s => new ShiftDto(
            s.Id.ToString(),
            s.StartTime,
            s.AssignedEmployeeId?.ToString()
        ));
    }
}
