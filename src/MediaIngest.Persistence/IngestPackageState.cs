namespace MediaIngest.Persistence;

public sealed record IngestPackageState(
    string PackageId,
    string WorkflowInstanceId,
    string Status,
    DateTimeOffset UpdatedAt);
