namespace SmartShift.Application.Features.UserManagement.DeleteUser;

public sealed class DeleteUserResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string[] Errors { get; init; } = Array.Empty<string>();
}