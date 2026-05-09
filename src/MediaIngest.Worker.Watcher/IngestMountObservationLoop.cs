namespace MediaIngest.Worker.Watcher;

internal sealed class IngestMountObservationLoop
{
    private readonly IngestMountObservationLoopOptions options;
    private readonly IIngestPackageCandidateSink sink;
    private readonly IngestMountScanner scanner;
    private readonly Func<TimeSpan, CancellationToken, ValueTask> delay;
    private readonly Dictionary<string, string> observedPackageFingerprints = new(StringComparer.Ordinal);

    public IngestMountObservationLoop(
        IngestMountObservationLoopOptions options,
        IIngestPackageCandidateSink sink)
        : this(options, sink, static (interval, cancellationToken) =>
            new ValueTask(Task.Delay(interval, cancellationToken)))
    {
    }

    public IngestMountObservationLoop(
        IngestMountObservationLoopOptions options,
        IIngestPackageCandidateSink sink,
        Func<TimeSpan, CancellationToken, ValueTask> delay)
        : this(options, sink, new IngestMountScanner(), delay)
    {
    }

    public IngestMountObservationLoop(
        IngestMountObservationLoopOptions options,
        IIngestPackageCandidateSink sink,
        IngestMountScanner scanner,
        Func<TimeSpan, CancellationToken, ValueTask> delay)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        this.delay = delay ?? throw new ArgumentNullException(nameof(delay));
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ScanOnceAsync(cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await delay(options.ScanInterval, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task ScanOnceAsync(CancellationToken cancellationToken)
    {
        var candidates = scanner.FindPackageCandidates(options.IngestMountPath);
        var currentPackagePaths = candidates
            .Select(candidate => candidate.PackagePath)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var observedPackagePath in observedPackageFingerprints.Keys.ToArray())
        {
            if (!currentPackagePaths.Contains(observedPackagePath))
            {
                observedPackageFingerprints.Remove(observedPackagePath);
            }
        }

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fingerprint = CreateObservationFingerprint(candidate);

            if (observedPackageFingerprints.TryGetValue(candidate.PackagePath, out var observedFingerprint)
                && StringComparer.Ordinal.Equals(observedFingerprint, fingerprint))
            {
                continue;
            }

            observedPackageFingerprints[candidate.PackagePath] = fingerprint;

            await sink.ObserveAsync(candidate, cancellationToken);
        }
    }

    private static string CreateObservationFingerprint(IngestPackageCandidate candidate)
    {
        var packagePath = Path.GetFullPath(candidate.PackagePath);

        return string.Join(
            '\n',
            Directory.EnumerateFiles(packagePath, "*", SearchOption.AllDirectories)
                .Select(filePath =>
                {
                    var fileInfo = new FileInfo(filePath);
                    return string.Join(
                        '|',
                        Path.GetRelativePath(packagePath, filePath),
                        fileInfo.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        fileInfo.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
                })
                .Order(StringComparer.Ordinal));
    }
}
