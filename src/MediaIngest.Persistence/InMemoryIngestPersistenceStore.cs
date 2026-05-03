namespace MediaIngest.Persistence;

public sealed class InMemoryIngestPersistenceStore : IIngestPersistenceStore
{
    private readonly List<IngestPackageState> packageStates = [];
    private readonly List<OutboxMessage> outboxMessages = [];

    public IReadOnlyList<IngestPackageState> PackageStates => packageStates;

    public IReadOnlyList<OutboxMessage> OutboxMessages => outboxMessages;

    public Task SaveAsync(PersistenceBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        cancellationToken.ThrowIfCancellationRequested();

        Validate(batch);

        packageStates.AddRange(batch.PackageStates);
        outboxMessages.AddRange(batch.OutboxMessages);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<OutboxMessage> pendingMessages = outboxMessages
            .Where(message => message.DispatchedAt is null)
            .ToArray();

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

        var messageIndex = outboxMessages.FindIndex(message => message.MessageId == messageId);

        if (messageIndex < 0)
        {
            throw new InvalidOperationException($"Outbox message '{messageId}' was not found.");
        }

        outboxMessages[messageIndex] = outboxMessages[messageIndex] with { DispatchedAt = dispatchedAt };

        return Task.CompletedTask;
    }

    private static void Validate(PersistenceBatch batch)
    {
        foreach (var packageState in batch.PackageStates)
        {
            if (string.IsNullOrWhiteSpace(packageState.PackageId))
            {
                throw new ArgumentException("Package state package id is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(packageState.WorkflowInstanceId))
            {
                throw new ArgumentException("Package state workflow instance id is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(packageState.Status))
            {
                throw new ArgumentException("Package state status is required.", nameof(batch));
            }
        }

        foreach (var outboxMessage in batch.OutboxMessages)
        {
            if (string.IsNullOrWhiteSpace(outboxMessage.MessageId))
            {
                throw new ArgumentException("Outbox message id is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.Destination))
            {
                throw new ArgumentException("Outbox message destination is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.MessageType))
            {
                throw new ArgumentException("Outbox message type is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.PayloadJson))
            {
                throw new ArgumentException("Outbox message payload is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.CorrelationId))
            {
                throw new ArgumentException("Outbox message correlation id is required.", nameof(batch));
            }
        }
    }
}
