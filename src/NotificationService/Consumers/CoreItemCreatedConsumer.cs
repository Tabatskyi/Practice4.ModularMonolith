using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        var correlationId = !string.IsNullOrWhiteSpace(message.CorrelationId)
            ? message.CorrelationId
            : context.CorrelationId?.ToString() ?? Guid.NewGuid().ToString();

        using var _ = _logger.BeginScope("CorrelationId:{CorrelationId}", correlationId);

        var notification = new NotificationEntity
        {
            EventId = message.EventId,
            CorrelationId = correlationId,
            Payload = JsonSerializer.Serialize(message, PayloadSerializerOptions),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Notifications.Add(notification);

        try
        {
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _logger.LogInformation(
                "Skipping duplicate notification event {EventId} for core item {CoreItemId}.",
                message.EventId,
                message.CoreItemId);
            return;
        }

        _logger.LogInformation(
            "Stored notification for event {EventId} and core item {CoreItemId}.",
            message.EventId,
            message.CoreItemId);
    }
}