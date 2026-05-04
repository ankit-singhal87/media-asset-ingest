namespace MediaIngest.Persistence;

public interface IIngestPersistenceStore
{
    Task SaveAsync(PersistenceBatch batch, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> ClaimPendingOutboxMessagesAsync(
        DateTimeOffset claimedAt,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken = default);

    Task MarkOutboxMessageDispatchedAsync(
        string messageId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default);
}
