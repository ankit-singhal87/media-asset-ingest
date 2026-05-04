using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow;

public static class PackageWorkflowGraphProjection
{
    private static readonly PreparedChildWork[] PackageStartPlan =
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
            MapLifecycleState(snapshot.State));
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

        return CreateGraph(packageId, workflowInstanceId, MapPackageStatus(status));
    }

    private static WorkflowGraphDto CreateGraph(
        string packageId,
        string workflowInstanceId,
        WorkflowNodeStatus packageStatus)
    {
        var childStatuses = MapChildStatuses(packageStatus);
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

        nodes.AddRange(PackageStartPlan.Select((work, index) => new WorkflowNodeDto(
            NodeId: work.NodeId,
            DisplayName: work.DisplayName,
            Kind: WorkflowNodeKind.Activity,
            Status: childStatuses[index],
            WorkflowInstanceId: workflowInstanceId,
            PackageId: packageId,
            WorkItemId: work.NodeId,
            ChildWorkflowInstanceId: null)));

        var edges = new[]
        {
            new WorkflowEdgeDto("package-start-scan-package", "package-start", "scan-package"),
            new WorkflowEdgeDto("scan-package-classify-files", "scan-package", "classify-files"),
            new WorkflowEdgeDto("classify-files-dispatch-processing", "classify-files", "dispatch-processing")
        };

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

    private static WorkflowNodeStatus[] MapChildStatuses(WorkflowNodeStatus packageStatus)
    {
        return packageStatus switch
        {
            WorkflowNodeStatus.Succeeded =>
            [
                WorkflowNodeStatus.Succeeded,
                WorkflowNodeStatus.Succeeded,
                WorkflowNodeStatus.Succeeded
            ],
            WorkflowNodeStatus.Failed =>
            [
                WorkflowNodeStatus.Failed,
                WorkflowNodeStatus.Failed,
                WorkflowNodeStatus.Failed
            ],
            WorkflowNodeStatus.Running =>
            [
                WorkflowNodeStatus.Running,
                WorkflowNodeStatus.Pending,
                WorkflowNodeStatus.Pending
            ],
            WorkflowNodeStatus.Queued =>
            [
                WorkflowNodeStatus.Pending,
                WorkflowNodeStatus.Pending,
                WorkflowNodeStatus.Pending
            ],
            _ =>
            [
                WorkflowNodeStatus.Pending,
                WorkflowNodeStatus.Pending,
                WorkflowNodeStatus.Pending
            ]
        };
    }
}
