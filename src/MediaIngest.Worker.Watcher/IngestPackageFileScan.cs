namespace MediaIngest.Worker.Watcher;

public sealed record IngestPackageFileScan(
    IReadOnlyList<IngestPackageFile> Files,
    IReadOnlyList<IngestPackageWarning> Warnings);
