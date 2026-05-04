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
            foreach (var packageState in batch.PackageStates)
            {
                var packageIndex = packageStates.FindIndex(existing => existing.PackageId == packageState.PackageId);

                if (packageIndex >= 0)
                {
                    packageStates[packageIndex] = packageState;
                }
                else
                {
                    packageStates.Add(packageState);
                }
            }

            foreach (var outboxMessage in batch.OutboxMessages)
            {
                if (!outboxMessages.Any(existing => existing.MessageId == outboxMessage.MessageId))
                {
                    outboxMessages.Add(outboxMessage);
                }
            }
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

    public Task<IReadOnlyList<OutboxMessage>> ClaimPendingOutboxMessagesAsync(
        DateTimeOffset claimedAt,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (claimExpiresAt <= claimedAt)
        {
            throw new ArgumentException("Outbox message claim expiry must be after the claim time.", nameof(claimExpiresAt));
        }

        IReadOnlyList<OutboxMessage> claimedMessages;

        lock (storeLock)
        {
            var claimableMessages = outboxMessages
                .Select((Message, Index) => (Message, Index))
                .Where(candidate =>
                    candidate.Message.DispatchedAt is null &&
                    (candidate.Message.DispatchClaimExpiresAt is null ||
                     candidate.Message.DispatchClaimExpiresAt <= claimedAt))
                .OrderBy(candidate => candidate.Message.CreatedAt)
                .ThenBy(candidate => candidate.Message.MessageId)
                .ToArray();

            claimedMessages = claimableMessages
                .Select(candidate => candidate.Message with { DispatchClaimExpiresAt = claimExpiresAt })
                .ToArray();

            foreach (var claimedMessage in claimedMessages)
            {
                var messageIndex = outboxMessages.FindIndex(message => message.MessageId == claimedMessage.MessageId);
                outboxMessages[messageIndex] = claimedMessage;
            }
        }

        return Task.FromResult(claimedMessages);
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

            if (outboxMessages[messageIndex].DispatchedAt is null)
            {
                outboxMessages[messageIndex] = outboxMessages[messageIndex] with
                {
                    DispatchedAt = dispatchedAt,
                    DispatchClaimExpiresAt = null
                };
            }
        }

        return Task.CompletedTask;
    }

}
