using MediaIngest.Workflow;

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

Console.WriteLine("MediaIngest workflow smoke tests passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}
