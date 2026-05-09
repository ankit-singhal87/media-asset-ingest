namespace MediaIngest.Api;

internal sealed class LocalManifestTransferConflictException(
    string packageId,
    string path)
    : InvalidOperationException($"Output file '{path}' already exists with different content for package '{packageId}'.")
{
    public string PackageId { get; } = packageId;

    public string Path { get; } = path;
}
