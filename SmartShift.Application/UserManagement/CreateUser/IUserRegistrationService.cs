using System.Threading;
using System.Threading.Tasks;

namespace SmartShift.Application.Features.UserManagement.CreateUser;

public interface IUserRegistrationService
{
    Task<CreateUserResult> RegisterUserAsync(CreateUserCommand command, CancellationToken cancellationToken);
}
