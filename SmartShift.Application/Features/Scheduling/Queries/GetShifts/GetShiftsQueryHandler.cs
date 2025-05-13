using MediatR;
using SmartShift.Infrastructure.Features.Scheduling.Repositories;

namespace SmartShift.Application.Features.Scheduling.Queries.GetShifts;

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, IEnumerable<ShiftDto>>
{
    private readonly IShiftRepository _shiftRepository;

    public GetShiftsQueryHandler(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public async Task<IEnumerable<ShiftDto>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        var startDate = DateTime.Parse(request.StartDate);
        var endDate = DateTime.Parse(request.EndDate);
        
        var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(startDate, endDate);
        
        return shifts.Select(s => new ShiftDto(
            s.Id.ToString(),
            s.StartTime,
            s.EndTime,
            s.AssignedEmployeeId?.ToString()
        ));
    }
} 