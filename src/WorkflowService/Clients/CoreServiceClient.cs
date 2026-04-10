using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WorkflowService.Clients;

internal sealed class CoreServiceClient(HttpClient httpClient) : ICoreServiceClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task UpdateListingStatusAsync(Guid listingId, CoreListingStatus status, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PatchAsJsonAsync(
            $"/core-items/{listingId}/status",
            new UpdateListingStatusRequest(status.ToString()),
            cancellationToken);

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
