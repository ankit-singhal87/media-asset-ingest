namespace MediaIngest.Contracts.Workflow;

public sealed record WorkflowGraphDto(
    string WorkflowInstanceId,
    string WorkflowName,
    string PackageId,
    string? ParentWorkflowInstanceId,
    IReadOnlyList<WorkflowNodeDto> Nodes,
    IReadOnlyList<WorkflowEdgeDto> Edges);
