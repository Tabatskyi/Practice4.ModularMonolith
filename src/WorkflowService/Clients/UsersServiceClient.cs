using System.Net;

namespace WorkflowService.Clients;

internal sealed class UsersServiceClient(HttpClient httpClient) : IUsersServiceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"/users/{userId}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"User with id '{userId}' was not found.");
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Users service validation failed for user '{userId}' ({(int)response.StatusCode} {response.ReasonPhrase}). {detail}".Trim());
    }
}
