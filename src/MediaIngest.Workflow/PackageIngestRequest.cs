namespace MediaIngest.Workflow;

public sealed record PackageIngestRequest(
    string PackageId,
    string PackagePath,
    string CorrelationId,
    DateTimeOffset AcceptedAt);
