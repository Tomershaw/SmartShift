using Carter;
using MediatR;
using Microsoft.AspNetCore.Http;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.Scheduling.RegisterForShift;

namespace SmartShift.Api.Features.Scheduling.Endpoints
{
    public class SchedulingModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/shifts/register", RegisterForShift)
                .WithName("RegisterForShift")
                .WithTags("Scheduling");
        }

        private static async Task<IResult> RegisterForShift(
         RegisterForShiftCommand command,
         IMediator mediator,
         HttpContext httpContext,
         ICurrentUserService currentUserService)
        {
            var userId = currentUserService.GetUserId();

            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return Results.BadRequest(new { Message = "Invalid Employee ID in token" });
            }

            command.UserId = userIdGuid;

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(new { Message = result.Message });
            }

            return Results.Ok(new { Message = result.Message });
        }

    }
}








