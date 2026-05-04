namespace MediaIngest.Api;

public sealed record IngestStartResponse(
    IReadOnlyList<StartedIngestPackageResponse> StartedPackages);

public sealed record IngestStartResult(
    IngestStartResponse Response,
    bool HasConflict);

public sealed record StartedIngestPackageResponse(
    string PackageId,
    string WorkflowInstanceId);

public sealed record IngestStatusResponse(
    IReadOnlyList<IngestPackageStatusResponse> Packages);

public sealed record IngestPackageStatusResponse(
    string PackageId,
    string WorkflowInstanceId,
    string Status,
    DateTimeOffset UpdatedAt);

public sealed record WorkflowListResponse(
    IReadOnlyList<WorkflowListItemResponse> Workflows);

public sealed record WorkflowListItemResponse(
    string WorkflowInstanceId,
    string PackageId,
    string Status,
    DateTimeOffset UpdatedAt);
