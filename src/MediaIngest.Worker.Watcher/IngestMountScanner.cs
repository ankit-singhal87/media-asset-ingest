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

    public IReadOnlyList<IngestPackageCandidate> FindReadyPackageCandidates(
        string ingestMountPath,
        ManifestReadinessGate readinessGate)
    {
        ArgumentNullException.ThrowIfNull(readinessGate);

        return FindPackageCandidates(ingestMountPath)
            .Where(readinessGate.IsReady)
            .ToArray();
    }

    public IReadOnlyList<IngestPackageFile> FindPackageFiles(IngestPackageCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var packagePath = Path.GetFullPath(candidate.PackagePath);

        return Directory.EnumerateFiles(packagePath, "*", SearchOption.AllDirectories)
            .Select(filePath => new IngestPackageFile(
                packagePath,
                Path.GetFullPath(filePath),
                Path.GetRelativePath(packagePath, filePath)))
            .OrderBy(file => file.PackageRelativePath, StringComparer.Ordinal)
            .ToArray();
    }
}
