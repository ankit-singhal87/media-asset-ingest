namespace MediaIngest.Workflow;

public sealed record PreparedChildWork(
    string NodeId,
    string DisplayName,
    string WorkflowName,
    string? WorkflowInstanceId = null,
    string? ParentWorkflowInstanceId = null);
