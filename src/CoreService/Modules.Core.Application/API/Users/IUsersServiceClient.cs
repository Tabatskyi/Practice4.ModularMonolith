namespace Modules.Core.Application.API.Users;

public interface IUsersServiceClient
{
    Task EnsureUserExists(Guid userId, CancellationToken cancellationToken = default);
}