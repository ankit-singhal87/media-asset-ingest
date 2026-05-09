namespace MediaIngest.Workflow;

internal sealed record PackageWorkflowLifecycleSnapshot(
    string PackageId,
    string PackagePath,
    string CorrelationId,
    PackageWorkflowLifecycleState State,
    DateTimeOffset OccurredAt,
    string? WorkflowInstanceId = null,
    string? FailureReason = null);
