namespace MediaIngest.Persistence;

public sealed record PersistenceBatch(
    IReadOnlyList<IngestPackageState> PackageStates,
    IReadOnlyList<OutboxMessage> OutboxMessages);
