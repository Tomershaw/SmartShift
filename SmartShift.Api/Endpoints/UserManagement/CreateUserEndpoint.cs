using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartShift.Api.Requests;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.UserManagement.CreateUser;
using System;
using System.Threading.Tasks;

namespace SmartShift.Api.Endpoints
{
    public class CreateUserEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/users/create", CreateUser)
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .WithName("CreateUser")
                .WithTags("UserManagement");
        }
            
        [Authorize(Roles = "Admin")] 
        private static async Task<IResult> CreateUser(
            CreateUserRequest request,
            IMediator mediator,
            HttpContext httpContext)
        {
            // קבלת TenantId מהטוקן של האדמין
            var adminTenantId = httpContext.User.FindFirst("tenantId")?.Value;

            if (string.IsNullOrEmpty(adminTenantId))
            {
                return Results.BadRequest(new { Message = "Tenant ID not found in token" });
            }

            // המרה של TenantId ל-Guid
            Guid tenantGuid;
            if (!Guid.TryParse(adminTenantId, out tenantGuid))
            {
                return Results.BadRequest(new { Message = "Invalid Tenant ID format" });
            }

            var command = new CreateUserCommand
            {
                Email = request.Email,
                FullName = request.FullName,
                Password = request.Password,
                TenantId = tenantGuid,
                Role = request.Role,
                PhoneNumber = request.PhoneNumber,
            };

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return Results.BadRequest(new { Errors = result.Errors });
            }

            return Results.Ok(new { Message = "המשתמש נוצר בהצלחה", UserId = result.UserId });
        }
    }
}