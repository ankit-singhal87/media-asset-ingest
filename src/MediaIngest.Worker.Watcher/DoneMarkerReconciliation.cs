namespace MediaIngest.Worker.Watcher;

public sealed record DoneMarkerReconciliation(
    IngestPackageCandidate Candidate,
    bool DoneMarkerObserved,
    IReadOnlyList<IngestPackageFile> Files);
