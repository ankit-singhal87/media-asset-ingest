namespace MediaIngest.Contracts.Workflow;

public sealed record WorkflowEdgeDto(
    string EdgeId,
    string SourceNodeId,
    string TargetNodeId);
