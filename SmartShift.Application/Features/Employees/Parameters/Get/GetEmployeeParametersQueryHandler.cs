using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Features.Employees.Parameters.Get;

public sealed class GetEmployeeParametersQueryHandler : IRequestHandler<GetEmployeeParametersQuery, EmployeeParametersDto?>
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public GetEmployeeParametersQueryHandler(ApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<EmployeeParametersDto?> Handle(GetEmployeeParametersQuery q, CancellationToken ct)
    {
        var tenantId = _current.GetTenantId();

        var dto = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == q.EmployeeId && e.TenantId == tenantId)
            .Select(e => new EmployeeParametersDto(
                e.SkillLevel,
                e.PriorityRating,
                e.MaxShiftsPerWeek,
                e.AdminNotes
            ))
            .FirstOrDefaultAsync(ct);

        return dto; // null => 404 ב־endpoint
    }
}
