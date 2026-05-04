using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow;

public static class PackageWorkflowGraphProjection
{
    private static readonly PreparedChildWork[] PackageLifecyclePlan =
    [
        new("scan-package", "Package scan"),
        new("classify-files", "Classify discovered files"),
        new("dispatch-processing", "Dispatch processing work"),
        new("reconcile-package", "Reconcile package"),
        new("finalize-package", "Finalize package")
    ];

    private static readonly PreparedChildWork[] PackageBusinessStatusPlan =
    [
        new("scan-package", "Package scan"),
        new("classify-files", "Classify discovered files"),
        new("dispatch-processing", "Dispatch processing work")
    ];

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
            PackageLifecyclePlan);
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
            PackageBusinessStatusPlan);
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
            Kind: WorkflowNodeKind.Activity,
            Status: childStatuses[index],
            WorkflowInstanceId: workflowInstanceId,
            PackageId: packageId,
            WorkItemId: work.NodeId,
            ChildWorkflowInstanceId: null)));

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
