using MediatR;
using SmartShift.Application.Features.UserManagement.CreateUser;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRegistrationService _registrationService;

    public CreateUserHandler(IUserRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await _registrationService.RegisterUserAsync(request, cancellationToken);
    }
}
