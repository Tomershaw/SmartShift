using SmartShift.Application.Features.Employees.GetEmployees;
using System;

namespace SmartShift.Application.Features.UserManagement.CreateUser;

public class CreateUserResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string[] Errors { get; set; } = Array.Empty<string>();
}
