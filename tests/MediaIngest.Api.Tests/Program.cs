using MediaIngest.Api;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;

var repoRoot = Path.Combine(Path.GetTempPath(), "media-ingest-api-tests", Guid.NewGuid().ToString("N"));
var inputPath = Path.Combine(repoRoot, "input");
var outputPath = Path.Combine(repoRoot, "output");
var packagePath = Path.Combine(inputPath, "asset-001");

Directory.CreateDirectory(packagePath);
Directory.CreateDirectory(outputPath);

try
{
    File.WriteAllText(Path.Combine(packagePath, "manifest.json"), "{garbage");
    File.WriteAllText(Path.Combine(packagePath, "manifest.json.checksum"), "opaque checksum");
    File.WriteAllText(Path.Combine(packagePath, "source.mov"), "media is not copied by local transfer");

    var runtimeService = CreateRuntimeService(inputPath, outputPath);

    var startResult = await runtimeService.StartAsync();

    AssertFalse(startResult.HasConflict, "start has conflict");
    AssertEqual(1, startResult.Response.StartedPackages.Count, "started package count");
    AssertEqual("asset-001", startResult.Response.StartedPackages[0].PackageId, "started package id");

    AssertTrue(File.Exists(Path.Combine(outputPath, "asset-001", "manifest.json")), "manifest copied");
    AssertTrue(File.Exists(Path.Combine(outputPath, "asset-001", "manifest.json.checksum")), "checksum copied");
    AssertFalse(File.Exists(Path.Combine(outputPath, "asset-001", "source.mov")), "source file not copied");

    var idempotentResult = await runtimeService.StartAsync();

    AssertFalse(idempotentResult.HasConflict, "idempotent start has conflict");

    File.WriteAllText(Path.Combine(outputPath, "asset-001", "manifest.json"), "different");

    var conflictingResult = await runtimeService.StartAsync();
    var conflictingStatus = runtimeService.GetStatus();

    AssertTrue(conflictingResult.HasConflict, "conflicting start has conflict");
    AssertEqual("Failed", conflictingStatus.Packages[0].Status, "conflicting package status");
    AssertEqual("different", File.ReadAllText(Path.Combine(outputPath, "asset-001", "manifest.json")), "conflicting output preserved");
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
        new PackageWorkflowStarter(),
        store,
        new OutboxDispatcher(store, publisher));
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
