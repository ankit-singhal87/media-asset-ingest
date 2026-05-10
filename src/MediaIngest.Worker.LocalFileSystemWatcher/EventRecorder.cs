using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class EventRecorder(
    WatcherDbContext context,
    CallbackTemplateRenderer templateRenderer,
    TimeProvider timeProvider)
{
    public async Task<WatchEvent> RecordAsync(
        ObservedFileSystemEvent observedEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observedEvent);

        var watch = await context.Watches.SingleAsync(
            candidate => candidate.WatchId == observedEvent.WatchId,
            cancellationToken);

        var values = new CallbackTemplateValues(
            observedEvent.EventType,
            observedEvent.IsFile,
            observedEvent.TargetEventSourcePath,
            observedEvent.OccurredAt);
        var callbackUrl = templateRenderer.Render(watch.CallbackUrlTemplate, values);
        var callbackPayload = templateRenderer.RenderPayloadJson(watch.CallbackPayloadTemplate, values);
        var eventId = CreateEventId(observedEvent);
        var createdAt = timeProvider.GetUtcNow();
        var watchEvent = new WatchEvent
        {
            EventId = eventId,
            WatchId = observedEvent.WatchId,
            EventType = observedEvent.EventType,
            IsFile = observedEvent.IsFile,
            TargetEventSourcePath = observedEvent.TargetEventSourcePath,
            OccurredAt = observedEvent.OccurredAt,
            CallbackUrl = callbackUrl,
            CallbackPayloadJson = callbackPayload,
            CreatedAt = createdAt
        };
        var outboxMessage = new CallbackOutboxMessage
        {
            MessageId = $"callback-{eventId}",
            EventId = eventId,
            Destination = callbackUrl,
            MessageType = "LocalFileSystemWatcherCallback",
            PayloadJson = JsonSerializer.Serialize(new
            {
                url = callbackUrl,
                payload = callbackPayload
            }),
            CreatedAt = createdAt
        };

        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        context.Events.Add(watchEvent);
        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return watchEvent;
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(
        CancellationToken cancellationToken)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return null;
        }

        return await context.Database.BeginTransactionAsync(cancellationToken);
    }

    private static string CreateEventId(ObservedFileSystemEvent observedEvent)
    {
        var input = string.Join(
            "|",
            observedEvent.WatchId,
            observedEvent.EventType,
            observedEvent.IsFile,
            observedEvent.TargetEventSourcePath,
            observedEvent.OccurredAt.ToUnixTimeMilliseconds(),
            Guid.NewGuid().ToString("N"));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
