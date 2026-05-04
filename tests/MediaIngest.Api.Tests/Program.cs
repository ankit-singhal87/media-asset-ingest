using MediaIngest.Api;
using MediaIngest.Contracts.Commands;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;
using System.Text.Json;

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
    var mediaPath = Directory.CreateDirectory(Path.Combine(packagePath, "media")).FullName;
    var sidecarPath = Directory.CreateDirectory(Path.Combine(packagePath, "sidecars")).FullName;
    File.WriteAllText(Path.Combine(packagePath, "manifest.json"), "{garbage");
    File.WriteAllText(Path.Combine(packagePath, "manifest.json.checksum"), "opaque checksum");
    File.WriteAllText(Path.Combine(mediaPath, "source.mov"), "media is not copied by local transfer");
    File.WriteAllText(Path.Combine(mediaPath, "mix.wav"), "audio essence");
    File.WriteAllText(Path.Combine(sidecarPath, "caption.srt"), "caption essence");
    File.WriteAllText(Path.Combine(packagePath, "notes.bin"), "unknown essence");

    await WaitForAsync(
        () => runtimeService.GetStatus().Packages.SingleOrDefault() is not null,
        "package created after start");

    AssertEqual("Started", runtimeService.GetStatus().Packages.Single().Status, "status before done marker");

    File.WriteAllText(Path.Combine(mediaPath, "late.mov"), "late video before done marker");
    File.WriteAllText(Path.Combine(packagePath, "done.marker"), string.Empty);

    await WaitForAsync(
        () => runtimeService.GetStatus().Packages.SingleOrDefault()?.Status == "Succeeded",
        "package to succeed after done marker");

    var startedStatus = runtimeService.GetStatus();
    AssertEqual(1, startedStatus.Packages.Count, "started package count");
    AssertEqual("asset-001", startedStatus.Packages[0].PackageId, "started package id");
    AssertEqual("Succeeded", startedStatus.Packages[0].Status, "started package status");

    var graph = runtimeService.GetWorkflowGraph("package-asset-001")
        ?? throw new InvalidOperationException("workflow graph endpoint source returned null.");
    AssertEqual("package-asset-001", graph.WorkflowInstanceId, "graph workflow instance id");
    AssertEqual("PackageIngestWorkflow", graph.WorkflowName, "graph workflow name");
    AssertEqual("asset-001", graph.PackageId, "graph package id");
    AssertEqual(11, graph.Nodes.Count, "graph node count");
    AssertEqual(10, graph.Edges.Count, "graph edge count");
    AssertEqual("package-start", graph.Nodes[0].NodeId, "graph first node id");
    AssertEqual("scan-package", graph.Nodes[1].NodeId, "graph scan node id");
    AssertEqual("classify-files", graph.Nodes[2].NodeId, "graph classify node id");
    AssertEqual("dispatch-processing", graph.Nodes[3].NodeId, "graph dispatch node id");
    AssertTrue(graph.Nodes.Any(node => node.NodeId == "command-media-mix-wav"), "audio command graph node");
    AssertTrue(graph.Nodes.Any(node => node.NodeId == "command-media-late-mov"), "late video command graph node");
    AssertTrue(graph.Nodes.Any(node => node.NodeId == "command-media-source-mov"), "video command graph node");
    AssertTrue(graph.Nodes.Any(node => node.NodeId == "command-sidecars-caption-srt"), "text command graph node");
    AssertTrue(graph.Nodes.Any(node => node.NodeId == "command-notes-bin"), "other command graph node");
    AssertEqual("reconcile-package", graph.Nodes[^2].NodeId, "graph reconcile node id");
    AssertEqual("finalize-package", graph.Nodes[^1].NodeId, "graph finalize node id");
    AssertEqual("Succeeded", graph.Nodes[0].Status.ToString(), "graph first node status");

    var mediaCommandMessages = runtime.Store.OutboxMessages
        .Where(message => message.MessageType == nameof(MediaCommandEnvelope))
        .OrderBy(message => message.MessageId, StringComparer.Ordinal)
        .ToArray();
    AssertEqual(5, mediaCommandMessages.Length, "media command message count");
    AssertTrue(mediaCommandMessages.All(message => message.DispatchedAt is not null), "media command messages dispatched");

    var commands = mediaCommandMessages
        .Select(message => JsonSerializer.Deserialize<MediaCommandEnvelope>(message.PayloadJson)
            ?? throw new InvalidOperationException("Media command envelope is required."))
        .OrderBy(command => command.InputPaths.Single(), StringComparer.Ordinal)
        .ToArray();

    AssertEqual(Path.Combine(packagePath, "media", "late.mov"), commands[0].InputPaths.Single(), "late video command input");
    AssertEqual(CommandNames.CreateProxy, commands[0].CommandName, "late video command name");
    AssertEqual(ExecutionClass.Light, commands[0].ExecutionClass, "late video command execution class");
    AssertEqual(Path.Combine(packagePath, "media", "mix.wav"), commands[1].InputPaths.Single(), "audio command input");
    AssertEqual(CommandNames.CreateProxy, commands[1].CommandName, "audio command name");
    AssertEqual(Path.Combine(packagePath, "media", "source.mov"), commands[2].InputPaths.Single(), "video command input");
    AssertEqual(CommandNames.CreateProxy, commands[2].CommandName, "video command name");
    AssertEqual(Path.Combine(packagePath, "notes.bin"), commands[3].InputPaths.Single(), "other command input");
    AssertEqual(CommandNames.CreateChecksum, commands[3].CommandName, "other command name");
    AssertEqual(Path.Combine(packagePath, "sidecars", "caption.srt"), commands[4].InputPaths.Single(), "text command input");
    AssertEqual(CommandNames.RunSecurityScan, commands[4].CommandName, "text command name");

    var missingGraph = runtimeService.GetWorkflowGraph("missing-workflow");
    AssertNull(missingGraph, "missing graph");

    var nodeDetails = runtimeService.GetWorkflowNodeDetails("package-asset-001", "scan-package")
        ?? throw new InvalidOperationException("workflow node details endpoint source returned null.");
    AssertEqual("package-asset-001", nodeDetails.WorkflowInstanceId, "node details workflow instance id");
    AssertEqual("scan-package", nodeDetails.NodeId, "node details node id");
    AssertEqual("Succeeded", nodeDetails.Timeline.Single().Status.ToString(), "node details timeline status");
    AssertEqual("correlation-asset-001", nodeDetails.Timeline.Single().CorrelationId, "node details timeline correlation");
    AssertEqual("Package scan found 7 files for asset-001.", nodeDetails.Timeline.Single().Message, "node details persisted timeline message");
    AssertEqual("Information", nodeDetails.Logs.Single().Level, "node details log level");
    AssertEqual("correlation-asset-001", nodeDetails.Logs.Single().CorrelationId, "node details log correlation");
    AssertEqual("Scan node persisted package file discovery for asset-001.", nodeDetails.Logs.Single().Message, "node details persisted log message");
    AssertFalse(nodeDetails.Logs.Single().Message.Contains("projected from in-memory workflow state", StringComparison.Ordinal), "node details log is not synthetic");

    var dispatchDetails = runtimeService.GetWorkflowNodeDetails("package-asset-001", "dispatch-processing")
        ?? throw new InvalidOperationException("dispatch node details endpoint source returned null.");
    AssertTrue(dispatchDetails.Timeline.Any(entry => entry.Message.Contains("Dispatched", StringComparison.Ordinal)), "dispatch node persisted timeline");

    var reconcileDetails = runtimeService.GetWorkflowNodeDetails("package-asset-001", "reconcile-package")
        ?? throw new InvalidOperationException("reconcile node details endpoint source returned null.");
    AssertTrue(reconcileDetails.Timeline.Any(entry => entry.Message.Contains("done.marker", StringComparison.Ordinal)), "reconcile node persisted timeline");

    var missingNodeDetails = runtimeService.GetWorkflowNodeDetails("package-asset-001", "missing-node");
    AssertNull(missingNodeDetails, "missing node details");

    AssertTrue(File.Exists(Path.Combine(outputPath, "asset-001", "manifest.json")), "manifest copied");
    AssertTrue(File.Exists(Path.Combine(outputPath, "asset-001", "manifest.json.checksum")), "checksum copied");
    AssertFalse(File.Exists(Path.Combine(outputPath, "asset-001", "media", "source.mov")), "source file not copied");

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

    var apiInputPath = Path.Combine(repoRoot, "api-input");
    var apiOutputPath = Path.Combine(repoRoot, "api-output");
    var apiPackagePath = Path.Combine(apiInputPath, "asset-http-001");
    Directory.CreateDirectory(apiPackagePath);
    File.WriteAllText(Path.Combine(apiPackagePath, "manifest.json"), "{\"id\":\"asset-http-001\"}");
    File.WriteAllText(Path.Combine(apiPackagePath, "manifest.json.checksum"), "opaque checksum");
    File.WriteAllText(Path.Combine(apiPackagePath, "clip.mp4"), "video essence");

    await using var freshApiHost = await IngestApiApplication.StartAsync(apiInputPath, apiOutputPath);
    using var emptyStatusResponse = await freshApiHost.HttpClient.GetAsync("/api/ingest/status");
    AssertEqual(System.Net.HttpStatusCode.OK, emptyStatusResponse.StatusCode, "fresh api status endpoint status");
    using var emptyStatusJson = JsonDocument.Parse(await emptyStatusResponse.Content.ReadAsStringAsync());
    AssertEqual(0, emptyStatusJson.RootElement.GetProperty("packages").GetArrayLength(), "fresh api status package count");

    using var acceptedStartResponse = await freshApiHost.HttpClient.PostAsync("/api/ingest/start", content: null);
    AssertEqual(System.Net.HttpStatusCode.Accepted, acceptedStartResponse.StatusCode, "fresh api start endpoint status");
    using var acceptedStartJson = JsonDocument.Parse(await acceptedStartResponse.Content.ReadAsStringAsync());
    var startedPackage = acceptedStartJson.RootElement.GetProperty("startedPackages").EnumerateArray().Single();
    AssertEqual("asset-http-001", startedPackage.GetProperty("packageId").GetString(), "fresh api started package id");
    AssertEqual("package-asset-http-001", startedPackage.GetProperty("workflowInstanceId").GetString(), "fresh api started workflow id");

    using var startedStatusResponse = await freshApiHost.HttpClient.GetAsync("/api/ingest/status");
    AssertEqual(System.Net.HttpStatusCode.OK, startedStatusResponse.StatusCode, "started api status endpoint status");
    using var startedStatusJson = JsonDocument.Parse(await startedStatusResponse.Content.ReadAsStringAsync());
    var statusPackage = startedStatusJson.RootElement.GetProperty("packages").EnumerateArray().Single();
    AssertEqual("asset-http-001", statusPackage.GetProperty("packageId").GetString(), "started api status package id");
    AssertEqual("package-asset-http-001", statusPackage.GetProperty("workflowInstanceId").GetString(), "started api status workflow id");
    AssertEqual("Started", statusPackage.GetProperty("status").GetString(), "started api status value");

    using var workflowListResponse = await freshApiHost.HttpClient.GetAsync("/api/workflows");
    AssertEqual(System.Net.HttpStatusCode.OK, workflowListResponse.StatusCode, "workflow list endpoint status");
    using var workflowListJson = JsonDocument.Parse(await workflowListResponse.Content.ReadAsStringAsync());
    var listedWorkflow = workflowListJson.RootElement.GetProperty("workflows").EnumerateArray().Single();
    AssertEqual("package-asset-http-001", listedWorkflow.GetProperty("workflowInstanceId").GetString(), "workflow list workflow id");
    AssertEqual("asset-http-001", listedWorkflow.GetProperty("packageId").GetString(), "workflow list package id");
    AssertEqual("Started", listedWorkflow.GetProperty("status").GetString(), "workflow list status");
    AssertTrue(listedWorkflow.TryGetProperty("updatedAt", out _), "workflow list updated at");

    await using var apiHost = await IngestApiApplication.StartAsync(inputPath, outputPath);
    using var apiStartResponse = await apiHost.HttpClient.PostAsync("/api/ingest/start", content: null);
    AssertEqual(System.Net.HttpStatusCode.Conflict, apiStartResponse.StatusCode, "api host start status");
    using var graphResponse = await apiHost.HttpClient.GetAsync("/api/workflows/package-asset-001/graph");
    AssertEqual(System.Net.HttpStatusCode.OK, graphResponse.StatusCode, "graph endpoint status");
    var graphJson = await graphResponse.Content.ReadAsStringAsync();
    AssertContains("\"workflowInstanceId\":\"package-asset-001\"", graphJson, "graph endpoint workflow instance id");
    AssertContains("\"packageId\":\"asset-001\"", graphJson, "graph endpoint package id");
    AssertContains("\"nodes\":[", graphJson, "graph endpoint node list");
    AssertContains("\"edges\":[", graphJson, "graph endpoint edge list");
    AssertContains("\"nodeId\":\"command-media-source-mov\"", graphJson, "graph endpoint command node");
    AssertContains("\"sourceNodeId\":\"package-start\"", graphJson, "graph endpoint edge source");

    using var nodeDetailsResponse = await apiHost.HttpClient.GetAsync("/api/workflows/package-asset-001/nodes/scan-package");
    AssertEqual(System.Net.HttpStatusCode.OK, nodeDetailsResponse.StatusCode, "node details endpoint status");
    var nodeDetailsJson = await nodeDetailsResponse.Content.ReadAsStringAsync();
    AssertContains("\"workflowInstanceId\":\"package-asset-001\"", nodeDetailsJson, "node details endpoint workflow instance id");
    AssertContains("\"nodeId\":\"scan-package\"", nodeDetailsJson, "node details endpoint node id");
    AssertContains("\"timeline\":[", nodeDetailsJson, "node details endpoint timeline");
    AssertContains("\"logs\":[", nodeDetailsJson, "node details endpoint logs");
    AssertContains("\"status\":3", nodeDetailsJson, "node details endpoint timeline status");
    AssertContains("\"level\":\"Information\"", nodeDetailsJson, "node details endpoint log level");
    AssertContains("\"correlationId\":\"correlation-asset-001\"", nodeDetailsJson, "node details endpoint correlation");
    AssertContains("Package scan found 7 files for asset-001.", nodeDetailsJson, "node details endpoint persisted message");

    using var missingNodeDetailsResponse = await apiHost.HttpClient.GetAsync("/api/workflows/package-asset-001/nodes/missing-node");
    AssertEqual(System.Net.HttpStatusCode.NotFound, missingNodeDetailsResponse.StatusCode, "missing node details endpoint status");

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
