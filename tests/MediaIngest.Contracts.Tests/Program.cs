using System.Text.Json;
using MediaIngest.Contracts.Commands;
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

AssertSequenceEqual(
    [
        "media.command.create_proxy",
        "media.command.create_checksum",
        "media.command.verify_checksum",
        "media.command.run_security_scan",
        "media.command.archive_asset"
    ],
    [
        CommandNames.CreateProxy,
        CommandNames.CreateChecksum,
        CommandNames.VerifyChecksum,
        CommandNames.RunSecurityScan,
        CommandNames.ArchiveAsset
    ],
    "stable command names");
AssertSequenceEqual(
    [
        "media.command.create_proxy",
        "media.command.create_checksum",
        "media.command.verify_checksum",
        "media.command.run_security_scan",
        "media.command.archive_asset"
    ],
    CommandBusTopology.CommandTopics,
    "stable command topology topic names");
AssertSequenceEqual(
    ["light", "medium", "heavy"],
    [
        CommandBusTopology.LightSubscriptionName,
        CommandBusTopology.MediumSubscriptionName,
        CommandBusTopology.HeavySubscriptionName
    ],
    "stable command topology subscription names");

AssertExecutionClassValues(
    [
        (ExecutionClass.Light, 0, "Light", "light"),
        (ExecutionClass.Medium, 1, "Medium", "medium"),
        (ExecutionClass.Heavy, 2, "Heavy", "heavy")
    ],
    "execution class values");
AssertEqual(ExecutionClass.Light, ExecutionClassProperties.FromPropertyValue("LIGHT"), "execution class property parse casing");
AssertJsonContains(ExecutionClass.Light, """
    "light"
    """, "light execution class JSON value");
AssertJsonContains(ExecutionClass.Medium, """
    "medium"
    """, "medium execution class JSON value");
AssertJsonContains(ExecutionClass.Heavy, """
    "heavy"
    """, "heavy execution class JSON value");

AssertEqual("PackageIngestWorkflow", WorkflowContractNames.PackageIngestWorkflow, "root workflow name");
AssertSequenceEqual(
    [
        "PackageScanWorkflow",
        "FileClassificationWorkflow",
        "EssenceGroupProcessingWorkflow",
        "ProxyCreationWorkflow",
        "ReconciliationWorkflow",
        "FinalizationWorkflow"
    ],
    [
        WorkflowContractNames.PackageScanWorkflow,
        WorkflowContractNames.FileClassificationWorkflow,
        WorkflowContractNames.EssenceGroupProcessingWorkflow,
        WorkflowContractNames.ProxyCreationWorkflow,
        WorkflowContractNames.ReconciliationWorkflow,
        WorkflowContractNames.FinalizationWorkflow
    ],
    "child workflow names");
AssertSequenceEqual(
    [
        "PackageIngestWorkflow",
        "PackageScanWorkflow",
        "FileClassificationWorkflow",
        "EssenceGroupProcessingWorkflow",
        "ProxyCreationWorkflow",
        "ReconciliationWorkflow",
        "FinalizationWorkflow"
    ],
    [
        WorkflowContractNames.PackageIngestWorkflow,
        WorkflowContractNames.PackageScanWorkflow,
        WorkflowContractNames.FileClassificationWorkflow,
        WorkflowContractNames.EssenceGroupProcessingWorkflow,
        WorkflowContractNames.ProxyCreationWorkflow,
        WorkflowContractNames.ReconciliationWorkflow,
        WorkflowContractNames.FinalizationWorkflow
    ],
    "stable workflow contract names");
AssertEnumValues<WorkflowNodeStatus>(
    [
        (WorkflowNodeStatus.Pending, 0, "Pending"),
        (WorkflowNodeStatus.Queued, 1, "Queued"),
        (WorkflowNodeStatus.Running, 2, "Running"),
        (WorkflowNodeStatus.Succeeded, 3, "Succeeded"),
        (WorkflowNodeStatus.Failed, 4, "Failed"),
        (WorkflowNodeStatus.Waiting, 5, "Waiting"),
        (WorkflowNodeStatus.Skipped, 6, "Skipped"),
        (WorkflowNodeStatus.Cancelled, 7, "Cancelled")
    ],
    "workflow node status values");
AssertEnumValues<WorkflowNodeKind>(
    [
        (WorkflowNodeKind.WorkflowStep, 0, "WorkflowStep"),
        (WorkflowNodeKind.Activity, 1, "Activity"),
        (WorkflowNodeKind.ChildWorkflow, 2, "ChildWorkflow"),
        (WorkflowNodeKind.WorkItem, 3, "WorkItem"),
        (WorkflowNodeKind.Wait, 4, "Wait"),
        (WorkflowNodeKind.CommandDispatch, 5, "CommandDispatch"),
        (WorkflowNodeKind.CommandCompletion, 6, "CommandCompletion"),
        (WorkflowNodeKind.Finalization, 7, "Finalization")
    ],
    "workflow node kind values");
AssertEqual(WorkflowNodeStatus.Running, graph.Nodes[1].Status, "node status");
AssertEqual("workflow-proxy-001", graph.Nodes[1].ChildWorkflowInstanceId, "child workflow drilldown link");
AssertEqual("scan", graph.Edges[0].SourceNodeId, "edge source");
AssertJsonRoundTrip(graph);
AssertDtoConstructor(
    typeof(WorkflowGraphDto),
    [
        ("WorkflowInstanceId", typeof(string)),
        ("WorkflowName", typeof(string)),
        ("PackageId", typeof(string)),
        ("ParentWorkflowInstanceId", typeof(string)),
        ("Nodes", typeof(IReadOnlyList<WorkflowNodeDto>)),
        ("Edges", typeof(IReadOnlyList<WorkflowEdgeDto>))
    ]);
AssertDtoConstructor(
    typeof(WorkflowNodeDto),
    [
        ("NodeId", typeof(string)),
        ("DisplayName", typeof(string)),
        ("Kind", typeof(WorkflowNodeKind)),
        ("Status", typeof(WorkflowNodeStatus)),
        ("WorkflowInstanceId", typeof(string)),
        ("PackageId", typeof(string)),
        ("WorkItemId", typeof(string)),
        ("ChildWorkflowInstanceId", typeof(string))
    ]);
AssertDtoConstructor(
    typeof(WorkflowEdgeDto),
    [
        ("EdgeId", typeof(string)),
        ("SourceNodeId", typeof(string)),
        ("TargetNodeId", typeof(string))
    ]);

var childGraph = graph with
{
    WorkflowInstanceId = "workflow-proxy-001",
    WorkflowName = WorkflowContractNames.ProxyCreationWorkflow,
    ParentWorkflowInstanceId = "workflow-package-001",
    Nodes = [],
    Edges = []
};
AssertEqual("workflow-package-001", childGraph.ParentWorkflowInstanceId, "child graph parent workflow reference");
AssertJsonRoundTrip(childGraph);

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
            Message: "Command runner accepted work",
            CorrelationId: "correlation-001",
            TraceId: "trace-001",
            SpanId: "span-001")
    ]);

AssertEqual("proxy", details.NodeId, "node details id");
AssertEqual("correlation-001", details.Timeline[0].CorrelationId, "timeline correlation");
AssertEqual("trace-001", details.Logs[0].TraceId, "log trace id");
AssertJsonRoundTrip(details);
AssertDtoConstructor(
    typeof(WorkflowNodeDetailsDto),
    [
        ("WorkflowInstanceId", typeof(string)),
        ("NodeId", typeof(string)),
        ("Timeline", typeof(IReadOnlyList<WorkflowTimelineEntryDto>)),
        ("Logs", typeof(IReadOnlyList<WorkflowNodeLogEntryDto>))
    ]);
AssertDtoConstructor(
    typeof(WorkflowTimelineEntryDto),
    [
        ("OccurredAt", typeof(DateTimeOffset)),
        ("Status", typeof(WorkflowNodeStatus)),
        ("Message", typeof(string)),
        ("CorrelationId", typeof(string))
    ]);
AssertDtoConstructor(
    typeof(WorkflowNodeLogEntryDto),
    [
        ("OccurredAt", typeof(DateTimeOffset)),
        ("Level", typeof(string)),
        ("Message", typeof(string)),
        ("CorrelationId", typeof(string)),
        ("TraceId", typeof(string)),
        ("SpanId", typeof(string))
    ]);

var lightRouting = CommandRoutingPolicy.Route(
    commandName: CommandNames.CreateProxy,
    inputBytes: 100L * 1024L * 1024L);
var mediumRouting = CommandRoutingPolicy.Route(
    commandName: CommandNames.CreateProxy,
    inputBytes: 2L * 1024L * 1024L * 1024L);
var heavyRouting = CommandRoutingPolicy.Route(
    commandName: CommandNames.CreateProxy,
    inputBytes: 25L * 1024L * 1024L * 1024L);

AssertEqual(CommandNames.CreateProxy, lightRouting.TopicName, "create proxy topic");
AssertEqual(ExecutionClass.Light, lightRouting.ExecutionClass, "light route");
AssertEqual("executionClass", CommandRoute.ExecutionClassPropertyName, "execution class property name");
AssertEqual("light", lightRouting.ApplicationProperties[CommandRoute.ExecutionClassPropertyName], "light route property");
AssertEqual(ExecutionClass.Medium, mediumRouting.ExecutionClass, "medium route");
AssertEqual("medium", mediumRouting.ApplicationProperties[CommandRoute.ExecutionClassPropertyName], "medium route property");
AssertEqual(ExecutionClass.Heavy, heavyRouting.ExecutionClass, "heavy route");
AssertEqual("heavy", heavyRouting.ApplicationProperties[CommandRoute.ExecutionClassPropertyName], "heavy route property");

var expectedCommandTopics = new[]
{
    CommandNames.CreateProxy,
    CommandNames.CreateChecksum,
    CommandNames.VerifyChecksum,
    CommandNames.RunSecurityScan,
    CommandNames.ArchiveAsset
};

var topology = CommandBusTopology.Topics;
AssertSequenceEqual(expectedCommandTopics, topology.Select(topic => topic.TopicName).ToArray(), "command bus topics");

foreach (var topic in topology)
{
    AssertEqual(3, topic.Subscriptions.Count, $"{topic.TopicName} subscription count");
    AssertSequenceEqual(
        ["light", "medium", "heavy"],
        topic.Subscriptions.Select(subscription => subscription.SubscriptionName).ToArray(),
        $"{topic.TopicName} subscription names");

    foreach (var subscription in topic.Subscriptions)
    {
        AssertEqual("executionClass", subscription.FilterPropertyName, $"{topic.TopicName}/{subscription.SubscriptionName} filter property");
        AssertEqual(subscription.SubscriptionName, subscription.FilterPropertyValue, $"{topic.TopicName}/{subscription.SubscriptionName} filter value");
        AssertEqual(
            $"executionClass = '{subscription.SubscriptionName}'",
            subscription.SqlFilterExpression,
            $"{topic.TopicName}/{subscription.SubscriptionName} filter expression");
    }
}

var checksumRouting = CommandRoutingPolicy.Route(
    commandName: CommandNames.CreateChecksum,
    inputBytes: 25L * 1024L * 1024L * 1024L);
AssertEqual(CommandNames.CreateChecksum, checksumRouting.TopicName, "checksum topic");
AssertEqual(ExecutionClass.Medium, checksumRouting.ExecutionClass, "checksum route");

var command = new MediaCommandEnvelope(
    CommandId: "command-001",
    CommandName: CommandNames.CreateProxy,
    TopicName: lightRouting.TopicName,
    ExecutionClass: lightRouting.ExecutionClass,
    CommandLine: "ffmpeg -i source.mov proxy.mp4",
    WorkingDirectory: "/mnt/work/package-001",
    InputPaths: ["/mnt/ingest/package-001/source.mov"],
    OutputPaths: ["/mnt/work/package-001/proxy.mp4"],
    CorrelationId: "correlation-001");

AssertEqual("media.command.create_proxy", command.TopicName, "command topic");
AssertEqual(ExecutionClass.Light, command.ExecutionClass, "command execution class");
AssertJsonContains(command, """
    "ExecutionClass":"light"
    """, "command execution class JSON");
AssertJsonRoundTrip(command);
AssertDtoConstructor(
    typeof(MediaCommandEnvelope),
    [
        ("CommandId", typeof(string)),
        ("CommandName", typeof(string)),
        ("TopicName", typeof(string)),
        ("ExecutionClass", typeof(ExecutionClass)),
        ("CommandLine", typeof(string)),
        ("WorkingDirectory", typeof(string)),
        ("InputPaths", typeof(IReadOnlyList<string>)),
        ("OutputPaths", typeof(IReadOnlyList<string>)),
        ("CorrelationId", typeof(string))
    ]);

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

static void AssertJsonContains<T>(T value, string expectedJsonFragment, string name)
{
    var json = JsonSerializer.Serialize(value);

    if (!json.Contains(expectedJsonFragment, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: expected JSON to contain '{expectedJsonFragment}', got '{json}'.");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"{name}: expected '{string.Join(", ", expected)}', got '{string.Join(", ", actual)}'.");
    }
}

static void AssertEnumValues<TEnum>(
    IReadOnlyList<(TEnum Value, int NumericValue, string Name)> expected,
    string name)
    where TEnum : struct, Enum
{
    AssertSequenceEqual(expected.Select(item => item.Name).ToArray(), Enum.GetNames<TEnum>(), $"{name} names");
    AssertSequenceEqual(expected.Select(item => item.Value).ToArray(), Enum.GetValues<TEnum>(), $"{name} order");

    foreach (var item in expected)
    {
        AssertEqual(item.NumericValue, Convert.ToInt32(item.Value), $"{name} numeric value for {item.Name}");
        AssertEqual(item.Name, item.Value.ToString(), $"{name} display name for {item.Name}");
    }
}

static void AssertExecutionClassValues(
    IReadOnlyList<(ExecutionClass Value, int NumericValue, string Name, string PropertyValue)> expected,
    string name)
{
    AssertEnumValues<ExecutionClass>(expected.Select(item => (item.Value, item.NumericValue, item.Name)).ToArray(), name);

    foreach (var item in expected)
    {
        AssertEqual(item.PropertyValue, item.Value.ToPropertyValue(), $"{name} property value for {item.Name}");
        AssertEqual(item.Value, ExecutionClassProperties.FromPropertyValue(item.PropertyValue), $"{name} property parse for {item.Name}");
    }
}

static void AssertDtoConstructor(
    Type dtoType,
    IReadOnlyList<(string Name, Type Type)> expectedParameters)
{
    var constructor = dtoType.GetConstructors().Single();
    var actualParameters = constructor.GetParameters()
        .Select(parameter => (Name: parameter.Name ?? string.Empty, Type: parameter.ParameterType))
        .ToArray();

    if (actualParameters.Length != expectedParameters.Count)
    {
        throw new InvalidOperationException(
            $"{dtoType.Name} constructor: expected {expectedParameters.Count} parameters, got {actualParameters.Length}.");
    }

    for (var i = 0; i < expectedParameters.Count; i++)
    {
        var expected = expectedParameters[i];
        var actual = actualParameters[i];

        AssertEqual(expected.Name, actual.Name, $"{dtoType.Name} constructor parameter {i + 1} name");
        AssertEqual(expected.Type, actual.Type, $"{dtoType.Name} constructor parameter {expected.Name} type");
    }
}
