namespace MediaIngest.Api;

internal sealed record LocalManifestTransferRequest(
    string PackageId,
    string PackagePath,
    string OutputRootPath);
