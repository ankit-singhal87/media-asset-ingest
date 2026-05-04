namespace MediaIngest.Worker.Watcher;

public sealed record IngestPackageFile(
    string PackagePath,
    string FilePath,
    string PackageRelativePath,
    long FileSizeBytes);
