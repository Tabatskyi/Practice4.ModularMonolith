using System.Net;
using Modules.Core.Application.API.Users;
using Shared.Api;

namespace Modules.Core.Infrastructure.Users;

internal sealed class UsersServiceClient(HttpClient httpClient) : IUsersServiceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task EnsureUserExists(Guid userId, CancellationToken cancellationToken = default)
    {
        try
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
                throw new UserNotFoundException(userId);
            }

            throw new UsersServiceUnavailableException(
                $"Users service returned status code {(int)response.StatusCode} ({response.ReasonPhrase}).");
        }
        catch (UserNotFoundException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UsersServiceUnavailableException("Users service request timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new UsersServiceUnavailableException("Users service is unreachable.", exception);
        }
    }
}