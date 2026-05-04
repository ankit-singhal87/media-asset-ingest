namespace MediaIngest.Worker.Watcher;

public sealed class IngestMountScanner
{
    private readonly ManifestReferenceReader manifestReferenceReader = new();

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

        return FindPhysicalPackageFiles(candidate);
    }

    public IngestPackageFileScan ScanPackageFiles(IngestPackageCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var files = FindPhysicalPackageFiles(candidate);
        var warnings = FindManifestWarnings(candidate, files);

        return new IngestPackageFileScan(files, warnings);
    }

    private static IReadOnlyList<IngestPackageFile> FindPhysicalPackageFiles(IngestPackageCandidate candidate)
    {
        var packagePath = Path.GetFullPath(candidate.PackagePath);

        return Directory.EnumerateFiles(packagePath, "*", SearchOption.AllDirectories)
            .Select(filePath => new IngestPackageFile(
                packagePath,
                Path.GetFullPath(filePath),
                Path.GetRelativePath(packagePath, filePath),
                new FileInfo(filePath).Length))
            .OrderBy(file => file.PackageRelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private IReadOnlyList<IngestPackageWarning> FindManifestWarnings(
        IngestPackageCandidate candidate,
        IReadOnlyList<IngestPackageFile> files)
    {
        var packagePath = Path.GetFullPath(candidate.PackagePath);
        var manifestPath = Path.Combine(packagePath, "manifest.json");

        if (!File.Exists(manifestPath))
        {
            return [];
        }

        var manifestReferences = manifestReferenceReader.ReadFileReferences(manifestPath);

        if (manifestReferences.MalformedWarning is not null)
        {
            return [manifestReferences.MalformedWarning];
        }

        var physicalRelativePaths = files
            .Select(file => file.PackageRelativePath)
            .ToHashSet(StringComparer.Ordinal);

        return manifestReferences.FilePaths
            .Where(reference => !physicalRelativePaths.Contains(reference))
            .Select(reference => new IngestPackageWarning(
                "ManifestFileMissing",
                reference,
                $"Manifest references missing file '{reference}'."))
            .ToArray();
    }

    public DoneMarkerReconciliation ReconcilePackageFilesOnDoneMarker(
        IngestPackageCandidate candidate,
        DoneMarkerReadinessGate doneMarkerGate)
    {
        ArgumentNullException.ThrowIfNull(doneMarkerGate);

        return new DoneMarkerReconciliation(
            candidate,
            doneMarkerGate.IsDone(candidate),
            FindPackageFiles(candidate));
    }
}
