using MediaIngest.Persistence;

namespace MediaIngest.Worker.Outbox;

public sealed class OutboxDispatcher(
    IIngestPersistenceStore persistenceStore,
    IOutboxMessagePublisher publisher,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider dispatchClock = timeProvider ?? TimeProvider.System;

    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await persistenceStore.GetPendingOutboxMessagesAsync(cancellationToken);
        var dispatchedCount = 0;

        foreach (var message in pendingMessages)
        {
            await publisher.PublishAsync(message, cancellationToken);
            await persistenceStore.MarkOutboxMessageDispatchedAsync(
                message.MessageId,
                dispatchClock.GetUtcNow(),
                cancellationToken);

            dispatchedCount++;
        }

        return dispatchedCount;
    }
}
