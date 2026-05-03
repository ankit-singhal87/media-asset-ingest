using System.Text.Json;
using MediaIngest.Contracts.Workflow;

var graph = new WorkflowGraphDto(
    WorkflowInstanceId: "workflow-package-001",
    WorkflowName: WorkflowContractNames.PackageIngestWorkflow,
    PackageId: "package-001",
    ParentWorkflowInstanceId: null,
    Nodes:
    [
        new WorkflowNodeDto(
            NodeId: "scan",
            DisplayName: "Package scan",
            Kind: WorkflowNodeKind.Activity,
            Status: WorkflowNodeStatus.Succeeded,
            WorkflowInstanceId: "workflow-package-001",
            PackageId: "package-001",
            WorkItemId: null,
            ChildWorkflowInstanceId: null),
        new WorkflowNodeDto(
            NodeId: "proxy",
            DisplayName: "Proxy creation",
            Kind: WorkflowNodeKind.ChildWorkflow,
            Status: WorkflowNodeStatus.Running,
            WorkflowInstanceId: "workflow-package-001",
            PackageId: "package-001",
            WorkItemId: "work-item-proxy",
            ChildWorkflowInstanceId: "workflow-proxy-001")
    ],
    Edges:
    [
        new WorkflowEdgeDto(
            EdgeId: "scan-to-proxy",
            SourceNodeId: "scan",
            TargetNodeId: "proxy")
    ]);

AssertEqual("PackageIngestWorkflow", WorkflowContractNames.PackageIngestWorkflow, "root workflow name");
AssertEqual(WorkflowNodeStatus.Running, graph.Nodes[1].Status, "node status");
AssertEqual("workflow-proxy-001", graph.Nodes[1].ChildWorkflowInstanceId, "child workflow drilldown link");
AssertEqual("scan", graph.Edges[0].SourceNodeId, "edge source");
AssertJsonRoundTrip(graph);

var details = new WorkflowNodeDetailsDto(
    WorkflowInstanceId: graph.WorkflowInstanceId,
    NodeId: "proxy",
    Timeline:
    [
        new WorkflowTimelineEntryDto(
            OccurredAt: new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero),
            Status: WorkflowNodeStatus.Running,
            Message: "Proxy creation started",
            CorrelationId: "correlation-001")
    ],
    Logs:
    [
        new WorkflowNodeLogEntryDto(
            OccurredAt: new DateTimeOffset(2026, 5, 3, 12, 1, 0, TimeSpan.Zero),
            Level: "Information",
            Message: "Proxy agent accepted work",
            CorrelationId: "correlation-001",
            TraceId: "trace-001",
            SpanId: "span-001")
    ]);

AssertEqual("proxy", details.NodeId, "node details id");
AssertEqual("correlation-001", details.Timeline[0].CorrelationId, "timeline correlation");
AssertEqual("trace-001", details.Logs[0].TraceId, "log trace id");
AssertJsonRoundTrip(details);

Console.WriteLine("MediaIngest contract smoke tests passed.");

static void AssertJsonRoundTrip<T>(T value)
{
    var json = JsonSerializer.Serialize(value);
    var roundTrip = JsonSerializer.Deserialize<T>(json);

    if (roundTrip is null)
    {
        throw new InvalidOperationException($"JSON round trip failed for {typeof(T).Name}.");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}
