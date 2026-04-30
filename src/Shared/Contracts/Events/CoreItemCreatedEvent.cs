using System.Text.Json.Serialization;

namespace Shared.Contracts.Events;

public record CoreItemCreatedEvent(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("occurredAt")] DateTimeOffset OccurredAt,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("coreItemId")] Guid CoreItemId,
    [property: JsonPropertyName("ownerUserId")] Guid OwnerUserId,
    [property: JsonPropertyName("summary")] string Summary);