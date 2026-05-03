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

var expectedDiagnosticEvents = new[]
{
    "ingest.scan",
    "ingest.readiness",
    "ingest.copy",
    "outbox.dispatch",
    "ingest.succeeded",
    "ingest.failed"
};

AssertSequenceEqual(expectedDiagnosticEvents, DiagnosticEventNames.All, "diagnostic event catalog");
AssertEqual("ingest.scan", DiagnosticEventNames.Scan, "scan diagnostic event");
AssertEqual("ingest.readiness", DiagnosticEventNames.Readiness, "readiness diagnostic event");
AssertEqual("ingest.copy", DiagnosticEventNames.Copy, "copy diagnostic event");
AssertEqual("outbox.dispatch", DiagnosticEventNames.OutboxDispatch, "outbox dispatch diagnostic event");
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
