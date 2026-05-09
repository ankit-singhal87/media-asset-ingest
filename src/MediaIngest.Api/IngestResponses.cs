namespace MediaIngest.Api;

internal sealed record IngestStartResponse(
    IReadOnlyList<StartedIngestPackageResponse> StartedPackages);

internal sealed record IngestStartResult(
    IngestStartResponse Response,
    bool HasConflict);

internal sealed record StartedIngestPackageResponse(
    string PackageId,
    string WorkflowInstanceId);

internal sealed record IngestStatusResponse(
    IReadOnlyList<IngestPackageStatusResponse> Packages);

internal sealed record IngestPackageStatusResponse(
    string PackageId,
    string WorkflowInstanceId,
    string Status,
    DateTimeOffset UpdatedAt);

internal sealed record WorkflowListResponse(
    IReadOnlyList<WorkflowListItemResponse> Workflows);

internal sealed record WorkflowListItemResponse(
    string WorkflowInstanceId,
    string PackageId,
    string Status,
    DateTimeOffset UpdatedAt);
