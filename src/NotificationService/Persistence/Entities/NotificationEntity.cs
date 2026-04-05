namespace NotificationService.Persistence.Entities;

public sealed class NotificationEntity
{
    public Guid EventId { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}