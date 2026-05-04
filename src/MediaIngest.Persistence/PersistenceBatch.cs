namespace MediaIngest.Persistence;

public sealed record PersistenceBatch(
    IReadOnlyList<IngestPackageState> PackageStates,
    IReadOnlyList<OutboxMessage> OutboxMessages,
    IReadOnlyList<BusinessTimelineRecord> TimelineRecords,
    IReadOnlyList<NodeDiagnosticLogRecord> NodeDiagnosticLogs)
{
    public PersistenceBatch(
        IReadOnlyList<IngestPackageState> packageStates,
        IReadOnlyList<OutboxMessage> outboxMessages)
        : this(packageStates, outboxMessages, [], [])
    {
    }
}
