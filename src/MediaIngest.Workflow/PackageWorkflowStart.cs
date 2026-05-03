namespace MediaIngest.Workflow;

public sealed record PackageWorkflowStart(
    string PackageId,
    string PackagePath,
    string WorkflowName,
    string WorkflowInstanceId,
    string CorrelationId,
    DateTimeOffset AcceptedAt,
    IReadOnlyList<PreparedChildWork> PreparedChildWork);
