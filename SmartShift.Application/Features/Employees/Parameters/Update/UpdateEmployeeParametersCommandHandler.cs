using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Infrastructure.Data;

namespace SmartShift.Application.Features.Employees.Parameters.Update;

public class UpdateEmployeeParametersCommandHandler
    : IRequestHandler<UpdateEmployeeParametersCommand, UpdateEmployeeParametersResult>
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _current;

    public UpdateEmployeeParametersCommandHandler(ApplicationDbContext db, ICurrentUserService current)
    {
        _db = db;
        _current = current;
    }

    public async Task<UpdateEmployeeParametersResult> Handle(UpdateEmployeeParametersCommand cmd, CancellationToken ct)
    {
        var tenantId = _current.GetTenantId();

        var emp = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == cmd.EmployeeId && e.TenantId == tenantId, ct);

        if (emp is null)
        {
            return new UpdateEmployeeParametersResult(
                Success: false,
                Message: "Employee not found in tenant.",
                EmployeeId: cmd.EmployeeId,
                Errors: new[] { "EMPLOYEE_NOT_FOUND" }
            );
        }

        // הוולידציה הלוגית כבר ב־Validator; כאן רק עדכון דומיין
        emp.UpdateSkillLevel(cmd.SkillLevel);
        emp.UpdatePriorityRating(cmd.PriorityRating);
        emp.UpdateMaxShiftsPerWeek(cmd.MaxShiftsPerWeek);
        if (cmd.AdminNotes is not null) emp.UpdateNotes(cmd.AdminNotes);

        await _db.SaveChangesAsync(ct);

        return new UpdateEmployeeParametersResult(
            Success: true,
            Message: string.Empty,
            EmployeeId: emp.Id,
            Errors: Array.Empty<string>()
        );
    }
}
