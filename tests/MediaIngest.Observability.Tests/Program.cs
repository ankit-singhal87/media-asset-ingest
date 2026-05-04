using MediaIngest.Observability;

var expectedFields = new[]
{
    "workflowInstanceId",
    "packageId",
    "fileId",
    "workItemId",
    "nodeId",
    "agentType",
    "queueName",
    "correlationId",
    "causationId",
    "traceId",
    "spanId"
};

AssertSequenceEqual(expectedFields, CorrelationFieldNames.All, "correlation field catalog");
AssertUnique(CorrelationFieldNames.All, "correlation field catalog");
AssertEqual("workflowInstanceId", CorrelationFieldNames.WorkflowInstanceId, "workflow instance field");
AssertEqual("packageId", CorrelationFieldNames.PackageId, "package field");
AssertEqual("fileId", CorrelationFieldNames.FileId, "file field");
AssertEqual("workItemId", CorrelationFieldNames.WorkItemId, "work item field");
AssertEqual("nodeId", CorrelationFieldNames.NodeId, "node field");
AssertEqual("agentType", CorrelationFieldNames.AgentType, "agent type field");
AssertEqual("queueName", CorrelationFieldNames.QueueName, "queue field");
AssertEqual("correlationId", CorrelationFieldNames.CorrelationId, "correlation field");
AssertEqual("causationId", CorrelationFieldNames.CausationId, "causation field");
AssertEqual("traceId", CorrelationFieldNames.TraceId, "trace field");
AssertEqual("spanId", CorrelationFieldNames.SpanId, "span field");

var expectedDiagnosticEvents = new[]
{
    "ingest.scan",
    "ingest.readiness",
    "ingest.copy",
    "outbox.dispatch",
    "command.started",
    "command.progress",
    "command.succeeded",
    "command.failed",
    "ingest.succeeded",
    "ingest.failed"
};

AssertSequenceEqual(expectedDiagnosticEvents, DiagnosticEventNames.All, "diagnostic event catalog");
AssertUnique(DiagnosticEventNames.All, "diagnostic event catalog");
AssertEqual("ingest.scan", DiagnosticEventNames.Scan, "scan diagnostic event");
AssertEqual("ingest.readiness", DiagnosticEventNames.Readiness, "readiness diagnostic event");
AssertEqual("ingest.copy", DiagnosticEventNames.Copy, "copy diagnostic event");
AssertEqual("outbox.dispatch", DiagnosticEventNames.OutboxDispatch, "outbox dispatch diagnostic event");
AssertEqual("command.started", DiagnosticEventNames.CommandStarted, "command started diagnostic event");
AssertEqual("command.progress", DiagnosticEventNames.CommandProgress, "command progress diagnostic event");
AssertEqual("command.succeeded", DiagnosticEventNames.CommandSucceeded, "command succeeded diagnostic event");
AssertEqual("command.failed", DiagnosticEventNames.CommandFailed, "command failed diagnostic event");
AssertEqual("ingest.succeeded", DiagnosticEventNames.Success, "success diagnostic event");
AssertEqual("ingest.failed", DiagnosticEventNames.Failure, "failure diagnostic event");

var context = new ObservabilityCorrelationContext(
    WorkflowInstanceId: "workflow-package-001",
    PackageId: "package-001",
    FileId: "file-source-001",
    WorkItemId: "work-item-proxy-001",
    NodeId: "node-proxy",
    AgentType: "proxy",
    QueueName: "mediaingest.proxy",
    CorrelationId: "correlation-001",
    CausationId: "command-001",
    TraceId: "trace-001",
    SpanId: "span-001");

var fields = context.ToFields();

foreach (var fieldName in CorrelationFieldNames.All)
{
    if (!fields.ContainsKey(fieldName))
    {
        throw new InvalidOperationException($"Missing correlation field '{fieldName}'.");
    }
}

AssertEqual("workflow-package-001", fields["workflowInstanceId"], "workflow correlation");
AssertEqual("package-001", fields["packageId"], "package correlation");
AssertEqual("proxy", fields["agentType"], "agent correlation");
AssertEqual("work-item-proxy-001", fields["workItemId"], "work item correlation");

var createdContext = ObservabilityCorrelationContext.Create(
    workflowInstanceId: "workflow-package-002",
    packageId: "package-002",
    fileId: "file-source-002",
    workItemId: "work-item-qc-002",
    nodeId: "node-qc",
    agentType: "qc",
    queueName: "mediaingest.qc",
    correlationId: "correlation-002",
    causationId: "command-002",
    traceId: "trace-002",
    spanId: "span-002");

var createdFields = createdContext.ToFields();

AssertEqual(CorrelationFieldNames.All.Count, createdFields.Count, "created context field count");
AssertEqual("workflow-package-002", createdFields[CorrelationFieldNames.WorkflowInstanceId], "created workflow correlation");
AssertEqual("package-002", createdFields[CorrelationFieldNames.PackageId], "created package correlation");
AssertEqual("file-source-002", createdFields[CorrelationFieldNames.FileId], "created file correlation");
AssertEqual("work-item-qc-002", createdFields[CorrelationFieldNames.WorkItemId], "created work item correlation");
AssertEqual("node-qc", createdFields[CorrelationFieldNames.NodeId], "created node correlation");
AssertEqual("qc", createdFields[CorrelationFieldNames.AgentType], "created agent correlation");
AssertEqual("mediaingest.qc", createdFields[CorrelationFieldNames.QueueName], "created queue correlation");
AssertEqual("correlation-002", createdFields[CorrelationFieldNames.CorrelationId], "created correlation id");
AssertEqual("command-002", createdFields[CorrelationFieldNames.CausationId], "created causation id");
AssertEqual("trace-002", createdFields[CorrelationFieldNames.TraceId], "created trace id");
AssertEqual("span-002", createdFields[CorrelationFieldNames.SpanId], "created span id");

AssertThrows<ArgumentException>(
    () => ObservabilityCorrelationContext.Create(
        workflowInstanceId: " ",
        packageId: "package-002",
        fileId: "file-source-002",
        workItemId: "work-item-qc-002",
        nodeId: "node-qc",
        agentType: "qc",
        queueName: "mediaingest.qc",
        correlationId: "correlation-002",
        causationId: "command-002",
        traceId: "trace-002",
        spanId: "span-002"),
    "blank workflow correlation");

Console.WriteLine("MediaIngest observability smoke test passed.");

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    if (expected.Count != actual.Count)
    {
        throw new InvalidOperationException($"{name}: expected {expected.Count} values, got {actual.Count}.");
    }

    for (var index = 0; index < expected.Count; index++)
    {
        AssertEqual(expected[index], actual[index], $"{name} value {index}");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertUnique(IReadOnlyList<string> values, string name)
{
    var seen = new HashSet<string>(StringComparer.Ordinal);

    foreach (var value in values)
    {
        if (!seen.Add(value))
        {
            throw new InvalidOperationException($"{name}: duplicate value '{value}'.");
        }
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
