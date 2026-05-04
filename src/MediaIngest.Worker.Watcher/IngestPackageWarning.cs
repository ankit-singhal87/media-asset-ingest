namespace MediaIngest.Worker.Watcher;

public sealed record IngestPackageWarning(
    string Code,
    string PackageRelativePath,
    string Message);
