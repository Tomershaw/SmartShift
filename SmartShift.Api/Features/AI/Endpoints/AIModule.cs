using Carter;
using Microsoft.SemanticKernel;
using SmartShift.Infrastructure.AI;
using SmartShift.Infrastructure.Repositories;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Domain.Features.Employees;
using SmartShift.Domain.Features.ShiftRegistrations; // EmployeeShiftAvailability

namespace SmartShift.Api.Features.AI.Endpoints;

public class AIModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/ai")
            .WithTags("AI")
            .RequireAuthorization();

        group.MapGet("/test-ai", TestAI)
            .WithName("TestAI")
            .WithSummary("Test if AI service is wired up");

        group.MapGet("/echo", Echo)
            .WithName("AIEcho")
            .WithSummary("Simple prompt echo to verify model connectivity");

        group.MapGet("/test-full-flow/{shiftId:guid}", TestFullFlow)
            .WithName("TestFullFlow")
            .WithSummary("Test full AI flow with real shift data");
    }

    private static IResult TestAI(IShiftAssignmentAIService aiService)
        => Results.Ok(new
        {
            Message = "AI service is configured and ready",
            Timestamp = DateTime.UtcNow
        });

    private static async Task<IResult> Echo([AsParameters] EchoRequest req, Kernel kernel, CancellationToken ct)
    {
        try
        {
            var prompt = string.IsNullOrWhiteSpace(req.Prompt)
                ? "Say OK if you can read me."
                : req.Prompt;

            var response = await kernel.InvokePromptAsync(prompt, cancellationToken: ct);
            var text = response.GetValue<string>() ?? "(empty)";

            return Results.Ok(new
            {
                Input = prompt,
                Output = text,
                Model = "OpenAI from appsettings",
                At = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"AI invocation failed: {ex.Message}");
        }
    }

    private static async Task<IResult> TestFullFlow(
        Guid shiftId,
        IShiftAssignmentAIService aiService,
        IShiftRepository shiftRepository,
        ICurrentUserService currentUserService,
        CancellationToken ct)
    {
        try
        {
            var tenantId = currentUserService.GetTenantId();

            var shift = await shiftRepository.GetByIdAsync(shiftId, tenantId, ct);
            if (shift is null)
                return Results.NotFound(new { error = "Shift not found", shiftId });

            // משיכת הרשמות ממתינות למשמרת
            var pendingRegistrations = await shiftRepository.GetPendingRegistrationsAsync(tenantId, shiftId, ct);
            var shiftRegistrations = pendingRegistrations
                .Where(r => r.ShiftId == shiftId && r.Employee != null)
                .ToList();

            if (!shiftRegistrations.Any())
            {
                return Results.Ok(new
                {
                    shiftId,
                    message = "No registered employees for this shift",
                    registeredCount = 0
                });
            }

            // בונים רשימת (Emp, Arrival) אמיתית לפי ההרשמות
            var peopleForAI = shiftRegistrations
                .Where(r => r.Employee != null)
                .GroupBy(r => r.Employee!.Id)
                .Select(g =>
                {
                    var emp = g.First().Employee!;
                    var arrival = g.Any(r => r.ShiftArrivalType == EmployeeShiftAvailability.Early)
                        ? EmployeeShiftAvailability.Early
                        : EmployeeShiftAvailability.Regular;
                    return (Emp: emp, Arrival: arrival);
                })
                .ToList();

            // 1) ניתוח דרישות
            var analysis = await aiService.AnalyzeShiftRequirementsAsync(shift, peopleForAI, ct);

            // 2) המלצה על עובדים - משתמשים באותה רשימה עם Arrival אמיתי
            var recommended = (await aiService.GetRecommendedEmployeesAsync(shift, peopleForAI, ct)).ToList();

            // 3) סיכום משמרת - משחזרים Arrival לכל עובד שהומלץ מהרשימה שבנינו
            var recommendedWithArrival = recommended.Select(emp =>
            {
                var pa = peopleForAI.First(p => p.Emp.Id == emp.Id);
                return (Emp: emp, Arrival: pa.Arrival);
            });

            var summary = await aiService.GenerateShiftSummaryAsync(shift, recommendedWithArrival, ct);

            return Results.Ok(new
            {
                shiftId,
                registeredCount = shiftRegistrations.Select(r => r.EmployeeId).Distinct().Count(),
                employeeDetails = peopleForAI.Select(x => new
                {
                    x.Emp.Id,
                    Name = $"{x.Emp.FirstName} {x.Emp.LastName}",
                    x.Emp.SkillLevel,
                    x.Emp.PriorityRating,
                    ArrivalType = x.Arrival.ToString()
                }),
                analysis,
                recommended = recommended.Select(e => new
                {
                    e.Id,
                    Name = $"{e.FirstName} {e.LastName}",
                    e.SkillLevel,
                    e.PriorityRating
                }),
                summary
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Full flow test failed: {ex.Message}");
        }
    }

    public record EchoRequest(string? Prompt);
}
