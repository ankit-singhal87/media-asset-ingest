using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow.Orchestrator;

public sealed class WorkflowGraphProjector(WorkflowDefinitionCatalog catalog)
{
    public static WorkflowGraphProjector CreateDefault()
    {
        return new WorkflowGraphProjector(WorkflowDefinitionCatalog.Discover(typeof(PackageIngestWorkflowDefinition).Assembly));
    }

    public WorkflowGraphDto ProjectDefinition(string workflowName)
    {
        var definition = catalog.GetRequired(workflowName);

        return ProjectInstance(new WorkflowGraphProjectionRequest(
            WorkflowName: definition.WorkflowName,
            WorkflowInstanceId: $"definition-{definition.WorkflowName}",
            PackageId: "definition",
            Status: WorkflowNodeStatus.Pending,
            ParentWorkflowInstanceId: null,
            CommandWorkItems: []));
    }

    public WorkflowGraphDto ProjectInstance(WorkflowGraphProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = catalog.GetRequired(request.WorkflowName);
        var commandWorkItems = request.CommandWorkItems.ToArray();
        var nodes = new List<WorkflowNodeDto>();

        foreach (var definitionNode in definition.Nodes)
        {
            if (definitionNode.NodeId == "command-work")
            {
                nodes.AddRange(commandWorkItems.Select(item => new WorkflowNodeDto(
                    NodeId: item.NodeId,
                    DisplayName: item.DisplayName,
                    Kind: WorkflowNodeKind.WorkItem,
                    Status: request.Status,
                    WorkflowInstanceId: request.WorkflowInstanceId,
                    PackageId: request.PackageId,
                    WorkItemId: item.WorkItemId,
                    ChildWorkflowInstanceId: null)));
                continue;
            }

            nodes.Add(new WorkflowNodeDto(
                NodeId: definitionNode.NodeId,
                DisplayName: definitionNode.DisplayName,
                Kind: definitionNode.Kind,
                Status: request.Status,
                WorkflowInstanceId: request.WorkflowInstanceId,
                PackageId: request.PackageId,
                WorkItemId: CreateWorkItemId(definitionNode),
                ChildWorkflowInstanceId: CreateChildWorkflowInstanceId(request.WorkflowInstanceId, definitionNode)));
        }

        return new WorkflowGraphDto(
            WorkflowInstanceId: request.WorkflowInstanceId,
            WorkflowName: definition.WorkflowName,
            PackageId: request.PackageId,
            ParentWorkflowInstanceId: request.ParentWorkflowInstanceId,
            Nodes: nodes,
            Edges: ProjectEdges(definition, commandWorkItems));
    }

    private static WorkflowEdgeDto[] ProjectEdges(
        WorkflowDefinition definition,
        IReadOnlyList<WorkflowCommandWorkItem> commandWorkItems)
    {
        var edges = new List<WorkflowEdgeDto>();

        foreach (var edge in definition.Edges)
        {
            if (edge.SourceNodeId == "command-work" || edge.TargetNodeId == "command-work")
            {
                continue;
            }

            edges.Add(new WorkflowEdgeDto(
                EdgeId: $"{edge.SourceNodeId}-{edge.TargetNodeId}",
                SourceNodeId: edge.SourceNodeId,
                TargetNodeId: edge.TargetNodeId));
        }

        foreach (var command in commandWorkItems)
        {
            edges.Add(new WorkflowEdgeDto(
                EdgeId: $"dispatch-processing-{command.NodeId}",
                SourceNodeId: "dispatch-processing",
                TargetNodeId: command.NodeId));
            edges.Add(new WorkflowEdgeDto(
                EdgeId: $"{command.NodeId}-wait-command-completion",
                SourceNodeId: command.NodeId,
                TargetNodeId: "wait-command-completion"));
        }

        if (commandWorkItems.Count == 0)
        {
            edges.Add(new WorkflowEdgeDto(
                EdgeId: "dispatch-processing-wait-command-completion",
                SourceNodeId: "dispatch-processing",
                TargetNodeId: "wait-command-completion"));
        }

        return [.. edges];
    }

    private static string? CreateWorkItemId(WorkflowDefinitionNode node)
    {
        return node.Kind is WorkflowNodeKind.WorkflowStep or WorkflowNodeKind.Wait
            ? null
            : node.NodeId;
    }

    private static string? CreateChildWorkflowInstanceId(string workflowInstanceId, WorkflowDefinitionNode node)
    {
        return node.Kind == WorkflowNodeKind.ChildWorkflow || node.Kind == WorkflowNodeKind.Finalization
            ? $"{workflowInstanceId}/{node.NodeId}"
            : null;
    }
}

public sealed record WorkflowGraphProjectionRequest(
    string WorkflowName,
    string WorkflowInstanceId,
    string PackageId,
    WorkflowNodeStatus Status,
    string? ParentWorkflowInstanceId,
    IReadOnlyList<WorkflowCommandWorkItem> CommandWorkItems);

public sealed record WorkflowCommandWorkItem(
    string NodeId,
    string DisplayName,
    string WorkItemId);
