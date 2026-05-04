using MediaIngest.Workflow;
using MediaIngest.Contracts.Workflow;

var request = new PackageIngestRequest(
    PackageId: "package-001",
    PackagePath: "/mnt/ingest/package-001",
    CorrelationId: "correlation-001",
    AcceptedAt: new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));

var starter = new PackageWorkflowStarter();
var start = starter.Start(request);

AssertEqual("package-001", start.PackageId, "package id");
AssertEqual("PackageIngestWorkflow", start.WorkflowName, "workflow name");
AssertEqual("package-package-001", start.WorkflowInstanceId, "workflow instance id");
AssertEqual("correlation-001", start.CorrelationId, "correlation id");
AssertEqual(6, start.PreparedChildWork.Count, "prepared child work count");
AssertEqual("scan-package", start.PreparedChildWork[0].NodeId, "scan node id");
AssertEqual("classify-files", start.PreparedChildWork[1].NodeId, "classify node id");
AssertEqual("essence-group-processing", start.PreparedChildWork[2].NodeId, "essence group node id");
AssertEqual("proxy-creation", start.PreparedChildWork[3].NodeId, "proxy node id");
AssertEqual("reconcile-package", start.PreparedChildWork[4].NodeId, "reconcile node id");
AssertEqual("finalize-package", start.PreparedChildWork[5].NodeId, "finalize node id");
AssertEqual(WorkflowContractNames.PackageScanWorkflow, start.PreparedChildWork[0].WorkflowName, "scan workflow name");
AssertEqual(WorkflowContractNames.FileClassificationWorkflow, start.PreparedChildWork[1].WorkflowName, "classification workflow name");
AssertEqual(WorkflowContractNames.EssenceGroupProcessingWorkflow, start.PreparedChildWork[2].WorkflowName, "essence group workflow name");
AssertEqual(WorkflowContractNames.ProxyCreationWorkflow, start.PreparedChildWork[3].WorkflowName, "proxy workflow name");
AssertEqual(WorkflowContractNames.ReconciliationWorkflow, start.PreparedChildWork[4].WorkflowName, "reconciliation workflow name");
AssertEqual(WorkflowContractNames.FinalizationWorkflow, start.PreparedChildWork[5].WorkflowName, "finalization workflow name");
AssertAll(
    start.PreparedChildWork,
    work => work.ParentWorkflowInstanceId == start.WorkflowInstanceId,
    "prepared child work carries parent workflow instance id");
AssertAll(
    start.PreparedChildWork,
    work => work.WorkflowInstanceId == $"{start.WorkflowInstanceId}/{work.NodeId}",
    "prepared child work carries stable child workflow instance id");

var startGraph = PackageWorkflowGraphProjection.FromWorkflowStart(start);
AssertEqual("package-package-001", startGraph.WorkflowInstanceId, "start graph workflow instance id");
AssertEqual(WorkflowNodeStatus.Queued, startGraph.Nodes[0].Status, "start graph package status");
AssertEqual("package-package-001/scan-package", startGraph.Nodes[1].ChildWorkflowInstanceId, "start graph scan child workflow instance id");

var lifecycle = PackageWorkflowLifecycle.Observe(request);
AssertEqual(PackageWorkflowLifecycleState.Observed, lifecycle.Current.State, "observed lifecycle state");
AssertEqual("package-001", lifecycle.Current.PackageId, "observed package id");
AssertEqual("correlation-001", lifecycle.Current.CorrelationId, "observed correlation id");

var ready = lifecycle.MarkReady(new DateTimeOffset(2026, 5, 3, 12, 1, 0, TimeSpan.Zero));
AssertEqual(PackageWorkflowLifecycleState.Ready, ready.State, "ready lifecycle state");

var lifecycleStart = lifecycle.Start(new DateTimeOffset(2026, 5, 3, 12, 2, 0, TimeSpan.Zero));
AssertEqual(PackageWorkflowLifecycleState.Started, lifecycle.Current.State, "started lifecycle state");
AssertEqual("package-package-001", lifecycleStart.WorkflowInstanceId, "started workflow instance id");

var succeeded = lifecycle.MarkSucceeded(new DateTimeOffset(2026, 5, 3, 12, 3, 0, TimeSpan.Zero));
AssertEqual(PackageWorkflowLifecycleState.Succeeded, succeeded.State, "succeeded lifecycle state");
AssertEqual(4, lifecycle.Events.Count, "succeeded lifecycle event count");
AssertEqual(PackageWorkflowLifecycleState.Observed, lifecycle.Events[0].State, "first lifecycle event");
AssertEqual(PackageWorkflowLifecycleState.Ready, lifecycle.Events[1].State, "second lifecycle event");
AssertEqual(PackageWorkflowLifecycleState.Started, lifecycle.Events[2].State, "third lifecycle event");
AssertEqual(PackageWorkflowLifecycleState.Succeeded, lifecycle.Events[3].State, "fourth lifecycle event");

var succeededGraph = PackageWorkflowGraphProjection.FromLifecycle(lifecycle);
AssertEqual("package-package-001", succeededGraph.WorkflowInstanceId, "succeeded graph workflow instance id");
AssertEqual("PackageIngestWorkflow", succeededGraph.WorkflowName, "succeeded graph workflow name");
AssertEqual("package-001", succeededGraph.PackageId, "succeeded graph package id");
AssertEqual(7, succeededGraph.Nodes.Count, "succeeded graph node count");
AssertEqual(6, succeededGraph.Edges.Count, "succeeded graph edge count");
AssertEqual("package-start", succeededGraph.Nodes[0].NodeId, "succeeded graph start node id");
AssertEqual(WorkflowNodeStatus.Succeeded, succeededGraph.Nodes[0].Status, "succeeded graph start status");
AssertEqual("scan-package", succeededGraph.Nodes[1].NodeId, "succeeded graph scan node id");
AssertEqual(WorkflowNodeKind.ChildWorkflow, succeededGraph.Nodes[1].Kind, "succeeded graph scan node kind");
AssertEqual(WorkflowNodeStatus.Succeeded, succeededGraph.Nodes[1].Status, "succeeded graph scan status");
AssertEqual("package-package-001/scan-package", succeededGraph.Nodes[1].ChildWorkflowInstanceId, "succeeded graph scan child workflow instance");
AssertEqual("reconcile-package", succeededGraph.Nodes[5].NodeId, "succeeded graph reconcile node id");
AssertEqual(WorkflowNodeStatus.Succeeded, succeededGraph.Nodes[5].Status, "succeeded graph reconcile status");
AssertEqual("package-package-001/reconcile-package", succeededGraph.Nodes[5].ChildWorkflowInstanceId, "succeeded graph reconcile child workflow instance");
AssertEqual("finalize-package", succeededGraph.Nodes[6].NodeId, "succeeded graph finalization node id");
AssertEqual(WorkflowNodeStatus.Succeeded, succeededGraph.Nodes[6].Status, "succeeded graph finalization status");
AssertEqual("package-package-001/finalize-package", succeededGraph.Nodes[6].ChildWorkflowInstanceId, "succeeded graph finalization child workflow instance");

var childWorkflowNodes = succeededGraph.Nodes
    .Where(node => node.Kind == WorkflowNodeKind.ChildWorkflow)
    .ToArray();
AssertEqual(6, childWorkflowNodes.Length, "parent graph child workflow node count");
AssertAll(
    childWorkflowNodes,
    node => node.WorkflowInstanceId == succeededGraph.WorkflowInstanceId,
    "parent child workflow node belongs to parent workflow");
AssertAll(
    childWorkflowNodes,
    node => node.PackageId == succeededGraph.PackageId,
    "parent child workflow node preserves package id");
AssertAll(
    childWorkflowNodes,
    node => node.WorkItemId == node.NodeId,
    "parent child workflow node exposes work item drilldown metadata");
AssertAll(
    childWorkflowNodes,
    node => node.ChildWorkflowInstanceId == $"{succeededGraph.WorkflowInstanceId}/{node.NodeId}",
    "parent child workflow node exposes child workflow drilldown metadata");

var scanChildGraph = PackageWorkflowGraphProjection.FromChildWorkflowNode(succeededGraph, succeededGraph.Nodes[1]);
AssertEqual("package-package-001/scan-package", scanChildGraph.WorkflowInstanceId, "scan child graph workflow instance id");
AssertEqual(WorkflowContractNames.PackageScanWorkflow, scanChildGraph.WorkflowName, "scan child graph workflow name");
AssertEqual("package-package-001", scanChildGraph.ParentWorkflowInstanceId, "scan child graph parent workflow instance id");
AssertEqual("package-001", scanChildGraph.PackageId, "scan child graph package id");
AssertEqual(2, scanChildGraph.Nodes.Count, "scan child graph node count");
AssertEqual(1, scanChildGraph.Edges.Count, "scan child graph edge count");
AssertEqual("scan-package-parent", scanChildGraph.Nodes[0].NodeId, "scan child graph parent navigation node id");
AssertEqual(WorkflowNodeKind.ChildWorkflow, scanChildGraph.Nodes[0].Kind, "scan child graph parent navigation node kind");
AssertEqual("package-package-001/scan-package", scanChildGraph.Nodes[0].WorkflowInstanceId, "scan child graph parent navigation workflow instance id");
AssertEqual("package-package-001", scanChildGraph.Nodes[0].ChildWorkflowInstanceId, "scan child graph parent navigation target");
AssertEqual("scan-package-root", scanChildGraph.Nodes[1].NodeId, "scan child graph root node id");
AssertEqual("scan-package", scanChildGraph.Nodes[1].WorkItemId, "scan child graph root work item metadata");
AssertEqual("scan-package-parent-scan-package-root", scanChildGraph.Edges[0].EdgeId, "scan child graph parent edge id");
AssertEqual("scan-package-parent", scanChildGraph.Edges[0].SourceNodeId, "scan child graph parent edge source");
AssertEqual("scan-package-root", scanChildGraph.Edges[0].TargetNodeId, "scan child graph parent edge target");

var childWorkflowNamesByNodeId = start.PreparedChildWork.ToDictionary(work => work.NodeId, work => work.WorkflowName);
foreach (var childWorkflowNode in childWorkflowNodes)
{
    var childGraph = PackageWorkflowGraphProjection.FromChildWorkflowNode(succeededGraph, childWorkflowNode);

    AssertEqual(childWorkflowNode.ChildWorkflowInstanceId, childGraph.WorkflowInstanceId, $"{childWorkflowNode.NodeId} child graph workflow instance id");
    AssertEqual(childWorkflowNamesByNodeId[childWorkflowNode.NodeId], childGraph.WorkflowName, $"{childWorkflowNode.NodeId} child graph workflow name");
    AssertEqual(succeededGraph.WorkflowInstanceId, childGraph.ParentWorkflowInstanceId, $"{childWorkflowNode.NodeId} child graph parent workflow instance id");
    AssertEqual(succeededGraph.PackageId, childGraph.PackageId, $"{childWorkflowNode.NodeId} child graph package id");
    AssertEqual(childWorkflowNode.Status, childGraph.Nodes[1].Status, $"{childWorkflowNode.NodeId} child graph root status");
    AssertEqual(childWorkflowNode.WorkItemId, childGraph.Nodes[1].WorkItemId, $"{childWorkflowNode.NodeId} child graph root work item id");
    AssertEqual(succeededGraph.WorkflowInstanceId, childGraph.Nodes[0].ChildWorkflowInstanceId, $"{childWorkflowNode.NodeId} child graph parent navigation target");
}

var failingLifecycle = PackageWorkflowLifecycle.Observe(request);
failingLifecycle.MarkReady(new DateTimeOffset(2026, 5, 3, 12, 4, 0, TimeSpan.Zero));
failingLifecycle.Start(new DateTimeOffset(2026, 5, 3, 12, 5, 0, TimeSpan.Zero));
var failed = failingLifecycle.MarkFailed(
    "classification command failed",
    new DateTimeOffset(2026, 5, 3, 12, 6, 0, TimeSpan.Zero));
AssertEqual(PackageWorkflowLifecycleState.Failed, failed.State, "failed lifecycle state");
AssertEqual("classification command failed", failed.FailureReason, "failure reason");

var failedGraph = PackageWorkflowGraphProjection.FromLifecycle(failingLifecycle);
AssertEqual(WorkflowNodeStatus.Failed, failedGraph.Nodes[0].Status, "failed graph start status");
AssertEqual(WorkflowNodeStatus.Failed, failedGraph.Nodes[1].Status, "failed graph scan status");

var readyGraph = PackageWorkflowGraphProjection.FromLifecycle(PackageWorkflowLifecycle.Observe(request).MarkReady(
    new DateTimeOffset(2026, 5, 3, 12, 8, 0, TimeSpan.Zero)));
AssertEqual("pending-package-001", readyGraph.WorkflowInstanceId, "ready graph fallback workflow instance id");
AssertEqual(WorkflowNodeStatus.Queued, readyGraph.Nodes[0].Status, "ready graph start status");
AssertEqual(WorkflowNodeStatus.Pending, readyGraph.Nodes[1].Status, "ready graph scan status");

var businessStatusGraph = PackageWorkflowGraphProjection.FromPackageStatus(
    packageId: "package-001",
    workflowInstanceId: "package-package-001",
    status: "Succeeded");
AssertEqual(4, businessStatusGraph.Nodes.Count, "business status graph node count");
AssertEqual("essence-group-processing", businessStatusGraph.Nodes[3].NodeId, "business status graph final visible node");

AssertThrows<InvalidOperationException>(
    () => PackageWorkflowLifecycle.Observe(request).Start(new DateTimeOffset(2026, 5, 3, 12, 7, 0, TimeSpan.Zero)),
    "start before ready");

Console.WriteLine("MediaIngest workflow smoke tests passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertThrows<TException>(Action action, string name)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"{name}: expected {typeof(TException).Name}.");
}

static void AssertAll<T>(IEnumerable<T> values, Func<T, bool> predicate, string name)
{
    var index = 0;
    foreach (var value in values)
    {
        if (!predicate(value))
        {
            throw new InvalidOperationException($"{name}: item {index} failed.");
        }

        index++;
    }
}
