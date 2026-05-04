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

var scanChildGraph = PackageWorkflowGraphProjection.FromChildWorkflowNode(succeededGraph, succeededGraph.Nodes[1]);
AssertEqual("package-package-001/scan-package", scanChildGraph.WorkflowInstanceId, "scan child graph workflow instance id");
AssertEqual(WorkflowContractNames.PackageScanWorkflow, scanChildGraph.WorkflowName, "scan child graph workflow name");
AssertEqual("package-package-001", scanChildGraph.ParentWorkflowInstanceId, "scan child graph parent workflow instance id");
AssertEqual("package-001", scanChildGraph.PackageId, "scan child graph package id");
AssertEqual(1, scanChildGraph.Nodes.Count, "scan child graph node count");
AssertEqual("scan-package-root", scanChildGraph.Nodes[0].NodeId, "scan child graph root node id");

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
