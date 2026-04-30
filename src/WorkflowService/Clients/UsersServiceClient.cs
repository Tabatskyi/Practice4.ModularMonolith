using System.Net;
using Shared.Api;

namespace WorkflowService.Clients;

internal sealed class UsersServiceClient(HttpClient httpClient) : IUsersServiceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/users/{userId}");
        var correlationId = CorrelationContext.CorrelationId ?? Guid.NewGuid().ToString();
        request.Headers.TryAddWithoutValidation(Utils.CorrelationIdHeaderName, correlationId);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

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
