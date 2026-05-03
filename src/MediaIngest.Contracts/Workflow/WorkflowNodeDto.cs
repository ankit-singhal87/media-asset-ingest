namespace MediaIngest.Contracts.Workflow;

public sealed record WorkflowNodeDto(
    string NodeId,
    string DisplayName,
    WorkflowNodeKind Kind,
    WorkflowNodeStatus Status,
    string WorkflowInstanceId,
    string PackageId,
    string? WorkItemId,
    string? ChildWorkflowInstanceId);
