using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow;

public static class PackageWorkflowGraphProjection
{
    public static WorkflowGraphDto FromLifecycle(PackageWorkflowLifecycle lifecycle)
    {
        ArgumentNullException.ThrowIfNull(lifecycle);

        return FromLifecycle(lifecycle.Current);
    }

    public static WorkflowGraphDto FromLifecycle(PackageWorkflowLifecycleSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var workflowInstanceId = string.IsNullOrWhiteSpace(snapshot.WorkflowInstanceId)
            ? $"pending-{snapshot.PackageId}"
            : snapshot.WorkflowInstanceId;

        return CreateGraph(
            snapshot.PackageId,
            workflowInstanceId,
            MapLifecycleState(snapshot.State),
            PackageWorkflowChildWorkPlan.FullLifecycle);
    }

    public static WorkflowGraphDto FromPackageStatus(
        string packageId,
        string workflowInstanceId,
        string status)
    {
        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package id is required.", nameof(packageId));
        }

        if (string.IsNullOrWhiteSpace(workflowInstanceId))
        {
            throw new ArgumentException("Workflow instance id is required.", nameof(workflowInstanceId));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Package status is required.", nameof(status));
        }

        return CreateGraph(
            packageId,
            workflowInstanceId,
            MapPackageStatus(status),
            PackageWorkflowChildWorkPlan.BusinessStatus);
    }

    public static WorkflowGraphDto FromChildWorkflowNode(
        WorkflowGraphDto parentGraph,
        WorkflowNodeDto childWorkflowNode)
    {
        ArgumentNullException.ThrowIfNull(parentGraph);
        ArgumentNullException.ThrowIfNull(childWorkflowNode);

        if (childWorkflowNode.Kind != WorkflowNodeKind.ChildWorkflow)
        {
            throw new ArgumentException("Node must be a child workflow node.", nameof(childWorkflowNode));
        }

        if (string.IsNullOrWhiteSpace(childWorkflowNode.ChildWorkflowInstanceId))
        {
            throw new ArgumentException("Child workflow instance id is required.", nameof(childWorkflowNode));
        }

        var childWork = PackageWorkflowChildWorkPlan.FindByNodeId(childWorkflowNode.NodeId)
            ?? throw new ArgumentException("Unknown child workflow node id.", nameof(childWorkflowNode));

        var parentNavigationNode = new WorkflowNodeDto(
            NodeId: $"{childWorkflowNode.NodeId}-parent",
            DisplayName: "Parent workflow",
            Kind: WorkflowNodeKind.ChildWorkflow,
            Status: childWorkflowNode.Status,
            WorkflowInstanceId: childWorkflowNode.ChildWorkflowInstanceId,
            PackageId: parentGraph.PackageId,
            WorkItemId: null,
            ChildWorkflowInstanceId: parentGraph.WorkflowInstanceId);

        var childRootNode = new WorkflowNodeDto(
            NodeId: $"{childWorkflowNode.NodeId}-root",
            DisplayName: childWorkflowNode.DisplayName,
            Kind: WorkflowNodeKind.WorkflowStep,
            Status: childWorkflowNode.Status,
            WorkflowInstanceId: childWorkflowNode.ChildWorkflowInstanceId,
            PackageId: parentGraph.PackageId,
            WorkItemId: childWorkflowNode.WorkItemId,
            ChildWorkflowInstanceId: null);

        return new WorkflowGraphDto(
            WorkflowInstanceId: childWorkflowNode.ChildWorkflowInstanceId,
            WorkflowName: childWork.WorkflowName,
            PackageId: parentGraph.PackageId,
            ParentWorkflowInstanceId: parentGraph.WorkflowInstanceId,
            Nodes: [parentNavigationNode, childRootNode],
            Edges:
            [
                new WorkflowEdgeDto(
                    EdgeId: $"{parentNavigationNode.NodeId}-{childRootNode.NodeId}",
                    SourceNodeId: parentNavigationNode.NodeId,
                    TargetNodeId: childRootNode.NodeId)
            ]);
    }

    private static WorkflowGraphDto CreateGraph(
        string packageId,
        string workflowInstanceId,
        WorkflowNodeStatus packageStatus,
        IReadOnlyList<PreparedChildWork> workflowPlan)
    {
        var childStatuses = MapChildStatuses(packageStatus, workflowPlan.Count);
        var nodes = new List<WorkflowNodeDto>
        {
            new(
                NodeId: "package-start",
                DisplayName: "Package ingest",
                Kind: WorkflowNodeKind.WorkflowStep,
                Status: packageStatus,
                WorkflowInstanceId: workflowInstanceId,
                PackageId: packageId,
                WorkItemId: null,
                ChildWorkflowInstanceId: null)
        };

        nodes.AddRange(workflowPlan.Select((work, index) => new WorkflowNodeDto(
            NodeId: work.NodeId,
            DisplayName: work.DisplayName,
            Kind: WorkflowNodeKind.ChildWorkflow,
            Status: childStatuses[index],
            WorkflowInstanceId: workflowInstanceId,
            PackageId: packageId,
            WorkItemId: work.NodeId,
            ChildWorkflowInstanceId: CreateChildWorkflowInstanceId(workflowInstanceId, work.NodeId))));

        var edges = CreateEdges(workflowPlan);

        return new WorkflowGraphDto(
            WorkflowInstanceId: workflowInstanceId,
            WorkflowName: WorkflowContractNames.PackageIngestWorkflow,
            PackageId: packageId,
            ParentWorkflowInstanceId: null,
            Nodes: nodes,
            Edges: edges);
    }

    private static WorkflowNodeStatus MapLifecycleState(PackageWorkflowLifecycleState state)
    {
        return state switch
        {
            PackageWorkflowLifecycleState.Observed => WorkflowNodeStatus.Pending,
            PackageWorkflowLifecycleState.Ready => WorkflowNodeStatus.Queued,
            PackageWorkflowLifecycleState.Started => WorkflowNodeStatus.Running,
            PackageWorkflowLifecycleState.Succeeded => WorkflowNodeStatus.Succeeded,
            PackageWorkflowLifecycleState.Failed => WorkflowNodeStatus.Failed,
            _ => WorkflowNodeStatus.Pending
        };
    }

    private static WorkflowNodeStatus MapPackageStatus(string status)
    {
        return status switch
        {
            "Started" => WorkflowNodeStatus.Running,
            "Succeeded" => WorkflowNodeStatus.Succeeded,
            "Failed" => WorkflowNodeStatus.Failed,
            _ => WorkflowNodeStatus.Pending
        };
    }

    private static WorkflowEdgeDto[] CreateEdges(IReadOnlyList<PreparedChildWork> workflowPlan)
    {
        var edges = new List<WorkflowEdgeDto>(workflowPlan.Count);
        var previousNodeId = "package-start";

        foreach (var work in workflowPlan)
        {
            edges.Add(new WorkflowEdgeDto(
                EdgeId: $"{previousNodeId}-{work.NodeId}",
                SourceNodeId: previousNodeId,
                TargetNodeId: work.NodeId));
            previousNodeId = work.NodeId;
        }

        return [.. edges];
    }

    private static string CreateChildWorkflowInstanceId(string parentWorkflowInstanceId, string nodeId)
    {
        return $"{parentWorkflowInstanceId}/{nodeId}";
    }

    private static WorkflowNodeStatus[] MapChildStatuses(WorkflowNodeStatus packageStatus, int childCount)
    {
        var childStatuses = Enumerable.Repeat(WorkflowNodeStatus.Pending, childCount).ToArray();

        if (packageStatus is WorkflowNodeStatus.Succeeded or WorkflowNodeStatus.Failed)
        {
            Array.Fill(childStatuses, packageStatus);
        }
        else if (packageStatus == WorkflowNodeStatus.Running && childStatuses.Length > 0)
        {
            childStatuses[0] = WorkflowNodeStatus.Running;
        }

        return childStatuses;
    }
}
