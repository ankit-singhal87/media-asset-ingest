namespace MediaIngest.Worker.Watcher;

public sealed class ManifestReadinessGate
{
    private const string ManifestFileName = "manifest.json";
    private const string ManifestChecksumFileName = "manifest.json.checksum";

    public bool IsReady(IngestPackageCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return File.Exists(Path.Combine(candidate.PackagePath, ManifestFileName))
            && File.Exists(Path.Combine(candidate.PackagePath, ManifestChecksumFileName));
    }
}
