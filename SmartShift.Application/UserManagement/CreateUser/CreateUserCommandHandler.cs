using MediatR;
using SmartShift.Application.Common.Interfaces;
using SmartShift.Application.Features.UserManagement.CreateUser;
using SmartShift.Domain.Data;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRegistrationService _registrationService;
    private readonly ICurrentUserService _currentUser;


    public CreateUserHandler(IUserRegistrationService registrationService, ICurrentUserService currentUser)
    {
        _registrationService = registrationService;
        _currentUser = currentUser;

    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.GetTenantId();
        return await _registrationService.RegisterUserAsync(request with { TenantId = tenantId }, cancellationToken);
    }
}
