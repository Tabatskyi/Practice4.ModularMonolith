namespace WorkflowService.Clients;

public interface IUsersServiceClient
{
    Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}
