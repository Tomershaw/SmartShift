using MediatR;

namespace SmartShift.Application.Features.UserManagement.DeleteUser;

public sealed class DeleteUserCommand : IRequest<DeleteUserResult>
{
    public required string UserId { get; init; }
    public required Guid TenantId { get; init; }
}