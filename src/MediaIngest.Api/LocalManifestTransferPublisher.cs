using System.Text.Json;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;

namespace MediaIngest.Api;

public sealed class LocalManifestTransferPublisher : IOutboxMessagePublisher
{
    private static readonly string[] ManifestFileNames =
    [
        "manifest.json",
        "manifest.json.checksum"
    ];

    public async Task PublishAsync(OutboxPublishRequest publishRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishRequest);
        cancellationToken.ThrowIfCancellationRequested();

        var request = JsonSerializer.Deserialize<LocalManifestTransferRequest>(publishRequest.Message.PayloadJson)
            ?? throw new InvalidOperationException("Local manifest transfer payload is required.");

        var outputPackagePath = Path.Combine(request.OutputRootPath, request.PackageId);
        Directory.CreateDirectory(outputPackagePath);

        foreach (var fileName in ManifestFileNames)
        {
            var sourcePath = Path.Combine(request.PackagePath, fileName);
            var destinationPath = Path.Combine(outputPackagePath, fileName);

            if (File.Exists(destinationPath))
            {
                if (await FilesAreEqualAsync(sourcePath, destinationPath, cancellationToken))
                {
                    continue;
                }

                throw new LocalManifestTransferConflictException(request.PackageId, destinationPath);
            }

            await using var source = File.OpenRead(sourcePath);
            await using var destination = File.Create(destinationPath);
            await source.CopyToAsync(destination, cancellationToken);
        }
    }

    private static async Task<bool> FilesAreEqualAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        var destinationBytes = await File.ReadAllBytesAsync(destinationPath, cancellationToken);

        return sourceBytes.AsSpan().SequenceEqual(destinationBytes);
    }
}
