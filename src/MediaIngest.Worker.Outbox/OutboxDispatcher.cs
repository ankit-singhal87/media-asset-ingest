using MediaIngest.Persistence;

namespace MediaIngest.Worker.Outbox;

public sealed class OutboxDispatcher(
    IIngestPersistenceStore persistenceStore,
    IOutboxMessagePublisher publisher,
    TimeProvider? timeProvider = null,
    TimeSpan? dispatchClaimDuration = null)
{
    private readonly TimeProvider dispatchClock = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan claimDuration = dispatchClaimDuration ?? TimeSpan.FromMinutes(5);

    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var claimedAt = dispatchClock.GetUtcNow();
        var pendingMessages = await persistenceStore.ClaimPendingOutboxMessagesAsync(
            claimedAt,
            claimedAt.Add(claimDuration),
            cancellationToken);
        var dispatchedCount = 0;

        foreach (var message in pendingMessages)
        {
            await publisher.PublishAsync(OutboxPublishRequest.From(message), cancellationToken);
            await persistenceStore.MarkOutboxMessageDispatchedAsync(
                message.MessageId,
                dispatchClock.GetUtcNow(),
                cancellationToken);

            dispatchedCount++;
        }

        return dispatchedCount;
    }
}
