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
AssertEqual(3, start.PreparedChildWork.Count, "prepared child work count");
AssertEqual("scan-package", start.PreparedChildWork[0].NodeId, "scan node id");
AssertEqual("classify-files", start.PreparedChildWork[1].NodeId, "classify node id");
AssertEqual("dispatch-processing", start.PreparedChildWork[2].NodeId, "dispatch node id");

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
AssertEqual(4, succeededGraph.Nodes.Count, "succeeded graph node count");
AssertEqual(3, succeededGraph.Edges.Count, "succeeded graph edge count");
AssertEqual("package-start", succeededGraph.Nodes[0].NodeId, "succeeded graph start node id");
AssertEqual(WorkflowNodeStatus.Succeeded, succeededGraph.Nodes[0].Status, "succeeded graph start status");
AssertEqual("scan-package", succeededGraph.Nodes[1].NodeId, "succeeded graph scan node id");
AssertEqual(WorkflowNodeStatus.Succeeded, succeededGraph.Nodes[1].Status, "succeeded graph scan status");

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
