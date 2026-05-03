namespace MediaIngest.Worker.Watcher;

public sealed class IngestMountScanner
{
    public IReadOnlyList<IngestPackageCandidate> FindPackageCandidates(string ingestMountPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ingestMountPath);

        return Directory.EnumerateDirectories(ingestMountPath)
            .Order(StringComparer.Ordinal)
            .Where(IsReadyForIngest)
            .Select(packagePath => new IngestPackageCandidate(Path.GetFullPath(packagePath)))
            .ToArray();
    }

    private static bool IsReadyForIngest(string packagePath)
    {
        return File.Exists(Path.Combine(packagePath, "manifest.json"))
            && File.Exists(Path.Combine(packagePath, "manifest.json.checksum"));
    }
}
