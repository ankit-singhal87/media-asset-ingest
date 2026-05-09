using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow.Orchestrator;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WorkflowDefinitionAttribute(string workflowName, string displayName) : Attribute
{
    public string WorkflowName { get; } = workflowName;

    public string DisplayName { get; } = displayName;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class WorkflowNodeAttribute(
    string nodeId,
    string displayName,
    WorkflowNodeKind kind,
    string? childWorkflowName = null) : Attribute
{
    public string NodeId { get; } = nodeId;

    public string DisplayName { get; } = displayName;

    public WorkflowNodeKind Kind { get; } = kind;

    public string? ChildWorkflowName { get; } = childWorkflowName;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class WorkflowEdgeAttribute(string sourceNodeId, string targetNodeId) : Attribute
{
    public string SourceNodeId { get; } = sourceNodeId;

    public string TargetNodeId { get; } = targetNodeId;
}
