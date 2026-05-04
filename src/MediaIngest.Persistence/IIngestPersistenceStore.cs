namespace MediaIngest.Persistence;

public interface IIngestPersistenceStore
{
    Task SaveAsync(PersistenceBatch batch, CancellationToken cancellationToken = default);

    Task<IngestPackageState?> GetPackageStateAsync(
        string packageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> ClaimPendingOutboxMessagesAsync(
        DateTimeOffset claimedAt,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken = default);

    Task MarkOutboxMessageDispatchedAsync(
        string messageId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessTimelineRecord>> GetWorkflowNodeTimelineAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeDiagnosticLogRecord>> GetWorkflowNodeLogsAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default);
}
