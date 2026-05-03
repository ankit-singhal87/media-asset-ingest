using MediaIngest.Worker.Watcher;

var mountPath = Path.Combine(Path.GetTempPath(), "media-ingest-watcher-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(mountPath);

try
{
    var firstPackagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-a")).FullName;
    var secondPackagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-b")).FullName;
    var readyPackagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-ready")).FullName;
    var readyPackageMediaPath = Directory.CreateDirectory(Path.Combine(readyPackagePath, "media")).FullName;
    var readyPackageSidecarPath = Directory.CreateDirectory(Path.Combine(readyPackagePath, "sidecars", "captions")).FullName;
    File.WriteAllText(Path.Combine(firstPackagePath, "manifest.json"), "{not-json");
    File.WriteAllText(Path.Combine(readyPackagePath, "manifest.json"), "{not-json");
    File.WriteAllText(Path.Combine(readyPackagePath, "manifest.json.checksum"), "not-a-real-checksum");
    File.WriteAllText(Path.Combine(readyPackageMediaPath, "clip.mov"), "not-real-media");
    File.WriteAllText(Path.Combine(readyPackageSidecarPath, "clip.en.srt"), "not-real-captions");
    File.WriteAllText(Path.Combine(mountPath, "loose-file.txt"), "not a package");

    var scanner = new IngestMountScanner();
    var readinessGate = new ManifestReadinessGate();

    var candidates = scanner.FindPackageCandidates(mountPath);

    AssertEqual(3, candidates.Count, "package candidate count");
    AssertSequenceEqual(
        [firstPackagePath, secondPackagePath, readyPackagePath],
        candidates.Select(candidate => candidate.PackagePath).ToArray(),
        "package candidate paths");

    AssertFalse(readinessGate.IsReady(new IngestPackageCandidate(firstPackagePath)), "manifest-only package readiness");
    AssertFalse(readinessGate.IsReady(new IngestPackageCandidate(secondPackagePath)), "empty package readiness");
    AssertTrue(readinessGate.IsReady(new IngestPackageCandidate(readyPackagePath)), "manifest and checksum package readiness");

    var discoveredFiles = scanner.FindPackageFiles(new IngestPackageCandidate(readyPackagePath));

    AssertSequenceEqual(
        [
            "manifest.json",
            "manifest.json.checksum",
            Path.Combine("media", "clip.mov"),
            Path.Combine("sidecars", "captions", "clip.en.srt"),
        ],
        discoveredFiles.Select(file => file.PackageRelativePath).ToArray(),
        "ready package discovered relative paths");

    AssertSequenceEqual(
        discoveredFiles.Select(file => Path.Combine(readyPackagePath, file.PackageRelativePath)).ToArray(),
        discoveredFiles.Select(file => file.FilePath).ToArray(),
        "ready package discovered full paths");
}
finally
{
    if (Directory.Exists(mountPath))
    {
        Directory.Delete(mountPath, recursive: true);
    }
}

Console.WriteLine("MediaIngest watcher smoke tests passed.");

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

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    AssertEqual(expected.Count, actual.Count, $"{name} length");

    for (var index = 0; index < expected.Count; index++)
    {
        AssertEqual(expected[index], actual[index], $"{name} item {index}");
    }
}
