namespace MediaIngest.Worker.Watcher;

public sealed class DoneMarkerReadinessGate
{
    public const string DoneMarkerFileName = "done.marker";

    public bool IsDone(IngestPackageCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var markerPath = Path.Combine(candidate.PackagePath, DoneMarkerFileName);

        return File.Exists(markerPath)
            && new FileInfo(markerPath).Length == 0;
    }
}
