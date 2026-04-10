using System.Net;
using System.Text.Json.Serialization;
using Shared.Api;

namespace WorkflowService.Clients;

internal sealed class CoreServiceClient(HttpClient httpClient) : ICoreServiceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task UpdateListingStatusAsync(Guid listingId, CoreListingStatus status, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/core-items/{listingId}/status")
        {
            Content = JsonContent.Create(new UpdateListingStatusRequest(status.ToString()))
        };
        var correlationId = CorrelationContext.CorrelationId ?? Guid.NewGuid().ToString();
        request.Headers.TryAddWithoutValidation(Utils.CorrelationIdHeaderName, correlationId);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Listing '{listingId}' was not found.");
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);

        throw new InvalidOperationException(
            $"Core service failed to update listing '{listingId}' to '{status}' ({(int)response.StatusCode} {response.ReasonPhrase}). {detail}".Trim());
    }

    private sealed record UpdateListingStatusRequest([property: JsonPropertyName("newStatus")] string NewStatus);
}
