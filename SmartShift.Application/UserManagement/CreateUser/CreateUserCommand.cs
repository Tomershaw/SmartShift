using MediatR;
using System;

namespace SmartShift.Application.Features.UserManagement.CreateUser;

public record CreateUserCommand : IRequest<CreateUserResult>
{
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string Role { get; init; } = "Employee"; // ברירת מחדל
    
}