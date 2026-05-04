namespace MediaIngest.Worker.Watcher;

public sealed class IngestMountObservationLoop
{
    private readonly IngestMountObservationLoopOptions options;
    private readonly IIngestPackageCandidateSink sink;
    private readonly IngestMountScanner scanner;
    private readonly Func<TimeSpan, CancellationToken, ValueTask> delay;
    private readonly HashSet<string> observedPackagePaths = new(StringComparer.Ordinal);

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
        foreach (var candidate in scanner.FindPackageCandidates(options.IngestMountPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!observedPackagePaths.Add(candidate.PackagePath))
            {
                continue;
            }

            await sink.ObserveAsync(candidate, cancellationToken);
        }
    }
}
