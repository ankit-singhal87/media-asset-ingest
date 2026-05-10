using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class EventRecorder(
    IWatchStore store,
    CallbackTemplateRenderer templateRenderer,
    TimeProvider timeProvider) : IEventRecorder
{
    public async Task<WatchEvent> RecordAsync(
        ObservedFileSystemEvent observedEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observedEvent);

        var watch = await store.FindWatchAsync(observedEvent.WatchId, cancellationToken)
            ?? throw new InvalidOperationException($"Watch '{observedEvent.WatchId}' does not exist.");

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

        await store.SaveWatchEventWithOutboxAsync(watchEvent, outboxMessage, cancellationToken);

        return watchEvent;
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
