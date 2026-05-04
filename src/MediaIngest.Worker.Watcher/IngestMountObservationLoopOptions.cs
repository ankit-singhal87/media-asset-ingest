namespace MediaIngest.Worker.Watcher;

public sealed record IngestMountObservationLoopOptions
{
    public IngestMountObservationLoopOptions(string ingestMountPath, TimeSpan scanInterval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ingestMountPath);

        if (scanInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(scanInterval), scanInterval, "Scan interval cannot be negative.");
        }

        IngestMountPath = Path.GetFullPath(ingestMountPath);
        ScanInterval = scanInterval;
    }

    public string IngestMountPath { get; }

    public TimeSpan ScanInterval { get; }
}
