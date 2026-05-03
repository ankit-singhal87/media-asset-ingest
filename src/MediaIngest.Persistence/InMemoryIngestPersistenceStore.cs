namespace MediaIngest.Persistence;

public sealed class InMemoryIngestPersistenceStore : IIngestPersistenceStore
{
    private readonly object storeLock = new();
    private readonly List<IngestPackageState> packageStates = [];
    private readonly List<OutboxMessage> outboxMessages = [];

    public IReadOnlyList<IngestPackageState> PackageStates
    {
        get
        {
            lock (storeLock)
            {
                return packageStates.ToArray();
            }
        }
    }

    public IReadOnlyList<OutboxMessage> OutboxMessages
    {
        get
        {
            lock (storeLock)
            {
                return outboxMessages.ToArray();
            }
        }
    }

    public Task SaveAsync(PersistenceBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        cancellationToken.ThrowIfCancellationRequested();

        IngestPersistenceBatchValidator.Validate(batch);

        lock (storeLock)
        {
            packageStates.AddRange(batch.PackageStates);
            outboxMessages.AddRange(batch.OutboxMessages);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<OutboxMessage> pendingMessages;

        lock (storeLock)
        {
            pendingMessages = outboxMessages
                .Where(message => message.DispatchedAt is null)
                .ToArray();
        }

        return Task.FromResult(pendingMessages);
    }

    public Task MarkOutboxMessageDispatchedAsync(
        string messageId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Outbox message id is required.", nameof(messageId));
        }

        lock (storeLock)
        {
            var messageIndex = outboxMessages.FindIndex(message => message.MessageId == messageId);

            if (messageIndex < 0)
            {
                throw new InvalidOperationException($"Outbox message '{messageId}' was not found.");
            }

            outboxMessages[messageIndex] = outboxMessages[messageIndex] with { DispatchedAt = dispatchedAt };
        }

        return Task.CompletedTask;
    }

}
