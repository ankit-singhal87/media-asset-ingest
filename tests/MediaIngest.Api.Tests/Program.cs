using MediaIngest.Api;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;

var repoRoot = Path.Combine(Path.GetTempPath(), "media-ingest-api-tests", Guid.NewGuid().ToString("N"));
var inputPath = Path.Combine(repoRoot, "input");
var outputPath = Path.Combine(repoRoot, "output");
Directory.CreateDirectory(outputPath);

try
{
    await using var runtimeService = CreateRuntimeService(inputPath, outputPath);
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
}
finally
{
    if (Directory.Exists(repoRoot))
    {
        Directory.Delete(repoRoot, recursive: true);
    }
}

Console.WriteLine("MediaIngest API smoke tests passed.");

static IngestRuntimeService CreateRuntimeService(string inputPath, string outputPath)
{
    var store = new InMemoryIngestPersistenceStore();
    var publisher = new LocalManifestTransferPublisher();

    return new IngestRuntimeService(
        new IngestRuntimePaths(inputPath, outputPath),
        new IngestMountScanner(),
        new ManifestReadinessGate(),
        new PackageWorkflowStarter(),
        store,
        new OutboxDispatcher(store, publisher));
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
