using System.Text.Json;
using MassTransit;
using NotificationService.Persistence;
using NotificationService.Persistence.Entities;
using Shared.Contracts.Events;

namespace NotificationService.Consumers;

public sealed class CoreItemCreatedConsumer(NotificationDbContext dbContext, ILogger<CoreItemCreatedConsumer> logger) : IConsumer<CoreItemCreatedEvent>
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly NotificationDbContext _dbContext = dbContext;
    private readonly ILogger<CoreItemCreatedConsumer> _logger = logger;

    public async Task Consume(ConsumeContext<CoreItemCreatedEvent> context)
    {
        var message = context.Message;

        var notification = new NotificationEntity
        {
            EventId = message.EventId,
            CorrelationId = message.CorrelationId,
            Payload = JsonSerializer.Serialize(message, PayloadSerializerOptions),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Stored notification for event {EventId} and core item {CoreItemId}.",
            message.EventId,
            message.CoreItemId);
    }
}