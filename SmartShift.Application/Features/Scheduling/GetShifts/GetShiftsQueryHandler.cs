using MediatR;
using Microsoft.Extensions.Logging;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Repositories;

namespace SmartShift.Application.Features.Scheduling.GetShifts;

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, IEnumerable<ShiftDto>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetShiftsQueryHandler> _logger;

    public GetShiftsQueryHandler(
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService,
        ILogger<GetShiftsQueryHandler> logger)
    {
        _shiftRepository = shiftRepository ?? throw new ArgumentNullException(nameof(shiftRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ShiftDto>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        // Input validation
        if (request == null)
        {
            _logger.LogWarning("Handle called with null request");
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.StartDate))
        {
            _logger.LogWarning("Handle called with null or empty StartDate");
            throw new ArgumentException("Start date cannot be null or empty", nameof(request.StartDate));
        }

        if (string.IsNullOrWhiteSpace(request.EndDate))
        {
            _logger.LogWarning("Handle called with null or empty EndDate");
            throw new ArgumentException("End date cannot be null or empty", nameof(request.EndDate));
        }

        // DateTime parsing with validation
        if (!DateTime.TryParse(request.StartDate, out var startDate))
        {
            _logger.LogWarning("Invalid start date format: {StartDate}", request.StartDate);
            throw new ArgumentException($"Invalid start date format: {request.StartDate}", nameof(request.StartDate));
        }

        if (!DateTime.TryParse(request.EndDate, out var endDate))
        {
            _logger.LogWarning("Invalid end date format: {EndDate}", request.EndDate);
            throw new ArgumentException($"Invalid end date format: {request.EndDate}", nameof(request.EndDate));
        }

        // Date logic validation
        if (startDate > endDate)
        {
            _logger.LogWarning("Start date {StartDate} is after end date {EndDate}", startDate, endDate);
            throw new ArgumentException("Start date cannot be after end date");
        }

        try
        {
            var tenantId = _currentUserService.GetTenantId();

            _logger.LogInformation("Getting shifts from {StartDate} to {EndDate} for tenant {TenantId}",
                startDate, endDate, tenantId);

            // ? שימוש בפונקציה הנכונה עם שני תאריכים
            var shifts = await _shiftRepository.GetShiftsInDateRangeAsync(startDate, endDate, tenantId, cancellationToken);

            var shiftDtos = shifts.Select(s => new ShiftDto(
                s.Id.ToString(),
                s.StartTime,
                s.AssignedEmployeeId?.ToString()
            )).ToList();

            _logger.LogInformation("Successfully retrieved {ShiftCount} shifts for tenant {TenantId}",
                shiftDtos.Count, tenantId);

            return shiftDtos;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetShifts operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shifts from {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }
}