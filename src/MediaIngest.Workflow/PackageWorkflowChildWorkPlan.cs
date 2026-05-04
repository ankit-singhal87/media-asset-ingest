using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow;

internal static class PackageWorkflowChildWorkPlan
{
    public static readonly PreparedChildWork ScanPackage = new(
        NodeId: "scan-package",
        DisplayName: "Package scan",
        WorkflowName: WorkflowContractNames.PackageScanWorkflow);

    public static readonly PreparedChildWork ClassifyFiles = new(
        NodeId: "classify-files",
        DisplayName: "Classify discovered files",
        WorkflowName: WorkflowContractNames.FileClassificationWorkflow);

    public static readonly PreparedChildWork EssenceGroupProcessing = new(
        NodeId: "essence-group-processing",
        DisplayName: "Essence group processing",
        WorkflowName: WorkflowContractNames.EssenceGroupProcessingWorkflow);

    public static readonly PreparedChildWork ProxyCreation = new(
        NodeId: "proxy-creation",
        DisplayName: "Proxy creation",
        WorkflowName: WorkflowContractNames.ProxyCreationWorkflow);

    public static readonly PreparedChildWork ReconcilePackage = new(
        NodeId: "reconcile-package",
        DisplayName: "Reconcile package",
        WorkflowName: WorkflowContractNames.ReconciliationWorkflow);

    public static readonly PreparedChildWork FinalizePackage = new(
        NodeId: "finalize-package",
        DisplayName: "Finalize package",
        WorkflowName: WorkflowContractNames.FinalizationWorkflow);

    public static readonly PreparedChildWork[] FullLifecycle =
    [
        ScanPackage,
        ClassifyFiles,
        EssenceGroupProcessing,
        ProxyCreation,
        ReconcilePackage,
        FinalizePackage
    ];

    public static readonly PreparedChildWork[] BusinessStatus =
    [
        ScanPackage,
        ClassifyFiles,
        EssenceGroupProcessing
    ];

    public static PreparedChildWork? FindByNodeId(string nodeId)
    {
        return FullLifecycle.FirstOrDefault(work => work.NodeId == nodeId);
    }

    public static PreparedChildWork[] PrepareForParent(string parentWorkflowInstanceId)
    {
        if (string.IsNullOrWhiteSpace(parentWorkflowInstanceId))
        {
            throw new ArgumentException("Parent workflow instance id is required.", nameof(parentWorkflowInstanceId));
        }

        return FullLifecycle
            .Select(work => work with
            {
                WorkflowInstanceId = CreateChildWorkflowInstanceId(parentWorkflowInstanceId, work.NodeId),
                ParentWorkflowInstanceId = parentWorkflowInstanceId
            })
            .ToArray();
    }

    private static string CreateChildWorkflowInstanceId(string parentWorkflowInstanceId, string nodeId)
    {
        return $"{parentWorkflowInstanceId}/{nodeId}";
    }
}
