using MediaIngest.Api;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;

var repoRoot = Path.Combine(Path.GetTempPath(), "media-ingest-api-tests", Guid.NewGuid().ToString("N"));

try
{
    var inputPath = Path.Combine(repoRoot, "success-input");
    var outputPath = Path.Combine(repoRoot, "success-output");
    Directory.CreateDirectory(outputPath);

    await using var runtime = CreateRuntimeService(inputPath, outputPath);
    var runtimeService = runtime.Service;
    var startResult = await runtimeService.StartAsync();

    AssertFalse(startResult.HasConflict, "initial start has conflict");
    AssertEqual(0, startResult.Response.StartedPackages.Count, "initial started package count");

    var packagePath = Path.Combine(inputPath, "asset-001");
    Directory.CreateDirectory(packagePath);
    File.WriteAllText(Path.Combine(packagePath, "manifest.json"), "{garbage");
    File.WriteAllText(Path.Combine(packagePath, "manifest.json.checksum"), "opaque checksum");
    File.WriteAllText(Path.Combine(packagePath, "source.mov"), "media is not copied by local transfer");

    await WaitForAsync(
        () => runtimeService.GetStatus().Packages.SingleOrDefault()?.Status == "Succeeded",
        "package created after start to succeed");

    var startedStatus = runtimeService.GetStatus();
    AssertEqual(1, startedStatus.Packages.Count, "started package count");
    AssertEqual("asset-001", startedStatus.Packages[0].PackageId, "started package id");
    AssertEqual("Succeeded", startedStatus.Packages[0].Status, "started package status");

    var graph = runtimeService.GetWorkflowGraph("package-asset-001")
        ?? throw new InvalidOperationException("workflow graph endpoint source returned null.");
    AssertEqual("package-asset-001", graph.WorkflowInstanceId, "graph workflow instance id");
    AssertEqual("PackageIngestWorkflow", graph.WorkflowName, "graph workflow name");
    AssertEqual("asset-001", graph.PackageId, "graph package id");
    AssertEqual(4, graph.Nodes.Count, "graph node count");
    AssertEqual(3, graph.Edges.Count, "graph edge count");
    AssertEqual("package-start", graph.Nodes[0].NodeId, "graph first node id");
    AssertEqual("scan-package", graph.Nodes[1].NodeId, "graph scan node id");
    AssertEqual("classify-files", graph.Nodes[2].NodeId, "graph classify node id");
    AssertEqual("dispatch-processing", graph.Nodes[3].NodeId, "graph dispatch node id");
    AssertEqual("Succeeded", graph.Nodes[0].Status.ToString(), "graph first node status");

    var missingGraph = runtimeService.GetWorkflowGraph("missing-workflow");
    AssertNull(missingGraph, "missing graph");

    AssertTrue(File.Exists(Path.Combine(outputPath, "asset-001", "manifest.json")), "manifest copied");
    AssertTrue(File.Exists(Path.Combine(outputPath, "asset-001", "manifest.json.checksum")), "checksum copied");
    AssertFalse(File.Exists(Path.Combine(outputPath, "asset-001", "source.mov")), "source file not copied");

    var stateCountAfterSuccess = runtimeService.GetStatus().Packages.Count;
    await Task.Delay(TimeSpan.FromMilliseconds(250));
    var idempotentStatus = runtimeService.GetStatus();

    AssertEqual(stateCountAfterSuccess, idempotentStatus.Packages.Count, "idempotent watcher package count");
    AssertEqual("Succeeded", idempotentStatus.Packages[0].Status, "idempotent watcher status");

    File.WriteAllText(Path.Combine(outputPath, "asset-001", "manifest.json"), "different");

    var repeatedStartResult = await runtimeService.StartAsync();
    var repeatedStartStatus = runtimeService.GetStatus();

    AssertFalse(repeatedStartResult.HasConflict, "repeated start has conflict");
    AssertEqual("Succeeded", repeatedStartStatus.Packages[0].Status, "repeated start status");
    AssertEqual("different", File.ReadAllText(Path.Combine(outputPath, "asset-001", "manifest.json")), "repeated start output preserved");

    var conflictInputPath = Path.Combine(repoRoot, "conflict-input");
    var conflictOutputPath = Path.Combine(repoRoot, "conflict-output");
    var conflictOutputPackagePath = Path.Combine(conflictOutputPath, "asset-001");
    Directory.CreateDirectory(conflictOutputPackagePath);
    File.WriteAllText(Path.Combine(conflictOutputPackagePath, "manifest.json"), "existing output");

    await using var conflictRuntime = CreateRuntimeService(conflictInputPath, conflictOutputPath);
    await conflictRuntime.Service.StartAsync();

    var conflictPackagePath = Path.Combine(conflictInputPath, "asset-001");
    Directory.CreateDirectory(conflictPackagePath);
    File.WriteAllText(Path.Combine(conflictPackagePath, "manifest.json"), "new input");
    File.WriteAllText(Path.Combine(conflictPackagePath, "manifest.json.checksum"), "opaque checksum");

    await WaitForAsync(
        () => conflictRuntime.Service.GetStatus().Packages.SingleOrDefault()?.Status == "Failed",
        "conflicting package created after start to fail");

    var conflictStatus = conflictRuntime.Service.GetStatus();
    AssertEqual(1, conflictStatus.Packages.Count, "conflict package count");
    AssertEqual("Failed", conflictStatus.Packages[0].Status, "conflict package status");
    AssertEqual("existing output", File.ReadAllText(Path.Combine(conflictOutputPackagePath, "manifest.json")), "conflict output preserved");

    await using var apiHost = await IngestApiApplication.StartAsync(inputPath, outputPath);
    using var apiStartResponse = await apiHost.HttpClient.PostAsync("/api/ingest/start", content: null);
    AssertEqual(System.Net.HttpStatusCode.Conflict, apiStartResponse.StatusCode, "api host start status");
    using var graphResponse = await apiHost.HttpClient.GetAsync("/api/workflows/package-asset-001/graph");
    AssertEqual(System.Net.HttpStatusCode.OK, graphResponse.StatusCode, "graph endpoint status");
    var graphJson = await graphResponse.Content.ReadAsStringAsync();
    AssertContains("\"workflowInstanceId\":\"package-asset-001\"", graphJson, "graph endpoint workflow instance id");
    AssertContains("\"packageId\":\"asset-001\"", graphJson, "graph endpoint package id");

    using var missingGraphResponse = await apiHost.HttpClient.GetAsync("/api/workflows/missing-workflow/graph");
    AssertEqual(System.Net.HttpStatusCode.NotFound, missingGraphResponse.StatusCode, "missing graph endpoint status");

    var stateCountAfterFailure = conflictRuntime.Store.PackageStates.Count;
    var messageCountAfterFailure = conflictRuntime.Store.OutboxMessages.Count;
    await Task.Delay(TimeSpan.FromMilliseconds(250));

    var conflictStatusAfterDelay = conflictRuntime.Service.GetStatus();
    AssertEqual("Failed", conflictStatusAfterDelay.Packages[0].Status, "conflict status after watcher intervals");
    AssertEqual("existing output", File.ReadAllText(Path.Combine(conflictOutputPackagePath, "manifest.json")), "conflict output preserved after watcher intervals");
    AssertEqual(stateCountAfterFailure, conflictRuntime.Store.PackageStates.Count, "conflict state count after watcher intervals");
    AssertEqual(messageCountAfterFailure, conflictRuntime.Store.OutboxMessages.Count, "conflict outbox message count after watcher intervals");
}
finally
{
    if (Directory.Exists(repoRoot))
    {
        Directory.Delete(repoRoot, recursive: true);
    }
}

Console.WriteLine("MediaIngest API smoke tests passed.");

static TestRuntime CreateRuntimeService(string inputPath, string outputPath)
{
    var store = new InMemoryIngestPersistenceStore();
    var publisher = new LocalManifestTransferPublisher();

    var service = new IngestRuntimeService(
        new IngestRuntimePaths(inputPath, outputPath),
        new IngestMountScanner(),
        new ManifestReadinessGate(),
        new PackageWorkflowStarter(),
        store,
        new OutboxDispatcher(store, publisher));

    return new TestRuntime(service, store);
}

static async Task WaitForAsync(Func<bool> condition, string name)
{
    var deadline = DateTimeOffset.UtcNow.AddSeconds(5);

    while (DateTimeOffset.UtcNow < deadline)
    {
        if (condition())
        {
            return;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(25));
    }

    throw new InvalidOperationException($"{name}: timed out.");
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true.");
    }
}

static void AssertFalse(bool condition, string name)
{
    if (condition)
    {
        throw new InvalidOperationException($"{name}: expected false.");
    }
}

static void AssertNull<T>(T? actual, string name)
{
    if (actual is not null)
    {
        throw new InvalidOperationException($"{name}: expected null.");
    }
}

static void AssertContains(string expectedSubstring, string actual, string name)
{
    if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: expected '{actual}' to contain '{expectedSubstring}'.");
    }
}

internal sealed record TestRuntime(
    IngestRuntimeService Service,
    InMemoryIngestPersistenceStore Store) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Service.DisposeAsync();
}
