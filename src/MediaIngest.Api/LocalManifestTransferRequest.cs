namespace MediaIngest.Api;

public sealed record LocalManifestTransferRequest(
    string PackageId,
    string PackagePath,
    string OutputRootPath);
