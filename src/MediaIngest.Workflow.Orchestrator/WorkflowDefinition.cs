using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow.Orchestrator;

public sealed record WorkflowDefinition(
    string WorkflowName,
    string DisplayName,
    IReadOnlyList<WorkflowDefinitionNode> Nodes,
    IReadOnlyList<WorkflowDefinitionEdge> Edges);

public sealed record WorkflowDefinitionNode(
    string NodeId,
    string DisplayName,
    WorkflowNodeKind Kind,
    string? ChildWorkflowName);

public sealed record WorkflowDefinitionEdge(
    string SourceNodeId,
    string TargetNodeId);

public sealed class WorkflowDefinitionCatalogException(string message) : Exception(message);
