namespace MediaIngest.Worker.Watcher;

public sealed class IngestMountScanner
{
    public IReadOnlyList<IngestPackageCandidate> FindPackageCandidates(string ingestMountPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ingestMountPath);

        return Directory.EnumerateDirectories(ingestMountPath)
            .Order(StringComparer.Ordinal)
            .Select(packagePath => new IngestPackageCandidate(Path.GetFullPath(packagePath)))
            .ToArray();
    }
}
