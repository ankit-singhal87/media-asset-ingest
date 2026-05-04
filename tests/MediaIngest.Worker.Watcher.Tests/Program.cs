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

    var readyCandidates = scanner.FindReadyPackageCandidates(mountPath, readinessGate);

    AssertSequenceEqual(
        [readyPackagePath],
        readyCandidates.Select(candidate => candidate.PackagePath).ToArray(),
        "ready package candidate paths before checksum arrival");

    File.WriteAllText(Path.Combine(firstPackagePath, "manifest.json.checksum"), "not-a-real-checksum");

    var readyCandidatesAfterChecksumArrival = scanner.FindReadyPackageCandidates(mountPath, readinessGate);

    AssertSequenceEqual(
        [firstPackagePath, readyPackagePath],
        readyCandidatesAfterChecksumArrival.Select(candidate => candidate.PackagePath).ToArray(),
        "ready package candidate paths after checksum arrival");

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

    var discoveredFileByRelativePath = discoveredFiles.ToDictionary(file => file.PackageRelativePath, StringComparer.Ordinal);
    AssertEqual(
        new FileInfo(Path.Combine(readyPackageMediaPath, "clip.mov")).Length,
        discoveredFileByRelativePath[Path.Combine("media", "clip.mov")].FileSizeBytes,
        "ready package discovered media file size");
    AssertEqual(
        new FileInfo(Path.Combine(readyPackageSidecarPath, "clip.en.srt")).Length,
        discoveredFileByRelativePath[Path.Combine("sidecars", "captions", "clip.en.srt")].FileSizeBytes,
        "ready package discovered sidecar file size");

    var doneMarkerGate = new DoneMarkerReadinessGate();
    var initialReconciliation = scanner.ReconcilePackageFilesOnDoneMarker(
        new IngestPackageCandidate(readyPackagePath),
        doneMarkerGate);

    AssertFalse(initialReconciliation.DoneMarkerObserved, "ready package initial done marker state");
    AssertSequenceEqual(
        discoveredFiles.Select(file => file.PackageRelativePath).ToArray(),
        initialReconciliation.Files.Select(file => file.PackageRelativePath).ToArray(),
        "ready package initial reconciliation relative paths");

    File.WriteAllText(Path.Combine(readyPackageMediaPath, "late.mov"), "late-media");
    File.WriteAllText(Path.Combine(readyPackagePath, "done.marker"), string.Empty);

    var finalReconciliation = scanner.ReconcilePackageFilesOnDoneMarker(
        new IngestPackageCandidate(readyPackagePath),
        doneMarkerGate);

    AssertTrue(finalReconciliation.DoneMarkerObserved, "ready package final done marker state");
    AssertSequenceEqual(
        [
            "done.marker",
            "manifest.json",
            "manifest.json.checksum",
            Path.Combine("media", "clip.mov"),
            Path.Combine("media", "late.mov"),
            Path.Combine("sidecars", "captions", "clip.en.srt"),
        ],
        finalReconciliation.Files.Select(file => file.PackageRelativePath).ToArray(),
        "ready package final reconciliation relative paths");

    await VerifyObservationLoopUsesConfiguredMountPath();
    await VerifyObservationLoopScansRepeatedlyUntilCancelled();
    await VerifyObservationLoopDoesNotReportUnchangedPackageTwice();
    await VerifyObservationLoopStopsWhenCancellationIsRequested();
    VerifyManifestDiscrepancyWarnings();
    VerifyMalformedManifestWarningDoesNotStopDiskDiscovery();
}
finally
{
    if (Directory.Exists(mountPath))
    {
        Directory.Delete(mountPath, recursive: true);
    }
}

Console.WriteLine("MediaIngest watcher smoke tests passed.");

static void VerifyManifestDiscrepancyWarnings()
{
    var packagePath = CreateTestDirectory();
    var mediaPath = Directory.CreateDirectory(Path.Combine(packagePath, "media")).FullName;
    var sidecarPath = Directory.CreateDirectory(Path.Combine(packagePath, "sidecars")).FullName;

    try
    {
        File.WriteAllText(
            Path.Combine(packagePath, "manifest.json"),
            """
            {
              "files": [
                "media/clip.mov",
                "missing/proxy.mp4"
              ]
            }
            """);
        File.WriteAllText(Path.Combine(packagePath, "manifest.json.checksum"), "not-a-real-checksum");
        File.WriteAllText(Path.Combine(mediaPath, "clip.mov"), "not-real-media");
        File.WriteAllText(Path.Combine(sidecarPath, "clip.en.srt"), "not-real-captions");

        var scan = new IngestMountScanner().ScanPackageFiles(new IngestPackageCandidate(packagePath));

        AssertSequenceEqual(
            new[]
            {
                "manifest.json",
                "manifest.json.checksum",
                Path.Combine("media", "clip.mov"),
                Path.Combine("sidecars", "clip.en.srt"),
            },
            scan.Files.Select(file => file.PackageRelativePath).ToArray(),
            "manifest discrepancy scan discovered relative paths");
        AssertEqual(1, scan.Warnings.Count, "manifest discrepancy warning count");
        AssertEqual("ManifestFileMissing", scan.Warnings[0].Code, "manifest discrepancy warning code");
        AssertEqual(
            Path.Combine("missing", "proxy.mp4"),
            scan.Warnings[0].PackageRelativePath,
            "manifest discrepancy warning relative path");
    }
    finally
    {
        DeleteTestDirectory(packagePath);
    }
}

static void VerifyMalformedManifestWarningDoesNotStopDiskDiscovery()
{
    var packagePath = CreateTestDirectory();

    try
    {
        File.WriteAllText(Path.Combine(packagePath, "manifest.json"), "{not-json");
        File.WriteAllText(Path.Combine(packagePath, "manifest.json.checksum"), "not-a-real-checksum");
        File.WriteAllText(Path.Combine(packagePath, "clip.mov"), "not-real-media");

        var scan = new IngestMountScanner().ScanPackageFiles(new IngestPackageCandidate(packagePath));

        AssertSequenceEqual(
            new[]
            {
                "clip.mov",
                "manifest.json",
                "manifest.json.checksum",
            },
            scan.Files.Select(file => file.PackageRelativePath).ToArray(),
            "malformed manifest scan discovered relative paths");
        AssertEqual(1, scan.Warnings.Count, "malformed manifest warning count");
        AssertEqual("ManifestMalformed", scan.Warnings[0].Code, "malformed manifest warning code");
        AssertEqual("manifest.json", scan.Warnings[0].PackageRelativePath, "malformed manifest warning relative path");
    }
    finally
    {
        DeleteTestDirectory(packagePath);
    }
}

static async Task VerifyObservationLoopUsesConfiguredMountPath()
{
    var firstMountPath = CreateTestDirectory();
    var secondMountPath = CreateTestDirectory();

    try
    {
        var ignoredPackagePath = Directory.CreateDirectory(Path.Combine(firstMountPath, "ignored-package")).FullName;
        var observedPackagePath = Directory.CreateDirectory(Path.Combine(secondMountPath, "observed-package")).FullName;
        var reports = new List<IngestPackageCandidate>();
        using var cancellation = new CancellationTokenSource();

        var loop = new IngestMountObservationLoop(
            new IngestMountObservationLoopOptions(secondMountPath, TimeSpan.FromMilliseconds(50)),
            new CallbackIngestPackageCandidateSink((candidate, _) =>
            {
                reports.Add(candidate);
                cancellation.Cancel();
                return ValueTask.CompletedTask;
            }));

        await loop.RunAsync(cancellation.Token);

        AssertSequenceEqual(
            [observedPackagePath],
            reports.Select(report => report.PackagePath).ToArray(),
            "observation loop configured mount reports");
        AssertFalse(
            reports.Any(report => report.PackagePath == ignoredPackagePath),
            "observation loop ignored mount reports");
    }
    finally
    {
        DeleteTestDirectory(firstMountPath);
        DeleteTestDirectory(secondMountPath);
    }
}

static async Task VerifyObservationLoopScansRepeatedlyUntilCancelled()
{
    var mountPath = CreateTestDirectory();

    try
    {
        var firstPackagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-a")).FullName;
        var secondPackagePath = Path.Combine(mountPath, "package-b");
        var reports = new List<IngestPackageCandidate>();
        var delayCalls = 0;
        using var cancellation = new CancellationTokenSource();

        var loop = new IngestMountObservationLoop(
            new IngestMountObservationLoopOptions(mountPath, TimeSpan.FromMilliseconds(50)),
            new CallbackIngestPackageCandidateSink((candidate, _) =>
            {
                reports.Add(candidate);
                return ValueTask.CompletedTask;
            }),
            (_, _) =>
            {
                delayCalls++;

                if (delayCalls == 1)
                {
                    Directory.CreateDirectory(secondPackagePath);
                }
                else
                {
                    cancellation.Cancel();
                }

                return ValueTask.CompletedTask;
            });

        await loop.RunAsync(cancellation.Token);

        AssertSequenceEqual(
            [firstPackagePath, Path.GetFullPath(secondPackagePath)],
            reports.Select(report => report.PackagePath).ToArray(),
            "observation loop repeated scan reports");
        AssertEqual(2, delayCalls, "observation loop repeated scan delay count");
    }
    finally
    {
        DeleteTestDirectory(mountPath);
    }
}

static async Task VerifyObservationLoopDoesNotReportUnchangedPackageTwice()
{
    var mountPath = CreateTestDirectory();

    try
    {
        var packagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-a")).FullName;
        var reports = new List<IngestPackageCandidate>();
        var delayCalls = 0;
        using var cancellation = new CancellationTokenSource();

        var loop = new IngestMountObservationLoop(
            new IngestMountObservationLoopOptions(mountPath, TimeSpan.FromMilliseconds(50)),
            new CallbackIngestPackageCandidateSink((candidate, _) =>
            {
                reports.Add(candidate);
                return ValueTask.CompletedTask;
            }),
            (_, _) =>
            {
                delayCalls++;

                if (delayCalls == 2)
                {
                    cancellation.Cancel();
                }

                return ValueTask.CompletedTask;
            });

        await loop.RunAsync(cancellation.Token);

        AssertSequenceEqual(
            [packagePath],
            reports.Select(report => report.PackagePath).ToArray(),
            "observation loop unchanged package reports");
        AssertEqual(2, delayCalls, "observation loop unchanged package delay count");
    }
    finally
    {
        DeleteTestDirectory(mountPath);
    }
}

static async Task VerifyObservationLoopStopsWhenCancellationIsRequested()
{
    var mountPath = CreateTestDirectory();

    try
    {
        var reports = new List<IngestPackageCandidate>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var loop = new IngestMountObservationLoop(
            new IngestMountObservationLoopOptions(mountPath, TimeSpan.FromMilliseconds(50)),
            new CallbackIngestPackageCandidateSink((candidate, _) =>
            {
                reports.Add(candidate);
                return ValueTask.CompletedTask;
            }));

        await loop.RunAsync(cancellation.Token);

        AssertEqual(0, reports.Count, "observation loop cancelled report count");
    }
    finally
    {
        DeleteTestDirectory(mountPath);
    }
}

static string CreateTestDirectory()
{
    var path = Path.Combine(Path.GetTempPath(), "media-ingest-watcher-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
}

static void DeleteTestDirectory(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
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

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    AssertEqual(expected.Count, actual.Count, $"{name} length");

    for (var index = 0; index < expected.Count; index++)
    {
        AssertEqual(expected[index], actual[index], $"{name} item {index}");
    }
}
