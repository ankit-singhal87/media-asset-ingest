using System.Text.Json;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;

namespace MediaIngest.Api;

public sealed class IngestRuntimeService(
    IngestRuntimePaths paths,
    IngestMountScanner scanner,
    PackageWorkflowStarter workflowStarter,
    InMemoryIngestPersistenceStore store,
    OutboxDispatcher outboxDispatcher)
{
    public async Task<IngestStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.InputPath);
        Directory.CreateDirectory(paths.OutputPath);

        var candidates = scanner.FindPackageCandidates(paths.InputPath);
        var startedPackages = new List<StartedIngestPackageResponse>();
        var hadTransferConflict = false;

        foreach (var candidate in candidates)
        {
            var packageId = Path.GetFileName(candidate.PackagePath);
            var acceptedAt = DateTimeOffset.UtcNow;
            var workflowStart = workflowStarter.Start(new PackageIngestRequest(
                PackageId: packageId,
                PackagePath: candidate.PackagePath,
                CorrelationId: $"correlation-{packageId}",
                AcceptedAt: acceptedAt));

            var message = CreateLocalTransferMessage(workflowStart, paths.OutputPath);

            await store.SaveAsync(new PersistenceBatch(
                [new IngestPackageState(packageId, workflowStart.WorkflowInstanceId, "Started", acceptedAt)],
                [message]), cancellationToken);

            startedPackages.Add(new StartedIngestPackageResponse(
                packageId,
                workflowStart.WorkflowInstanceId));

            try
            {
                await outboxDispatcher.DispatchPendingAsync(cancellationToken);
                await store.SaveAsync(new PersistenceBatch(
                    [new IngestPackageState(packageId, workflowStart.WorkflowInstanceId, "Succeeded", DateTimeOffset.UtcNow)],
                    []), cancellationToken);
            }
            catch (LocalManifestTransferConflictException)
            {
                hadTransferConflict = true;
                await store.SaveAsync(new PersistenceBatch(
                    [new IngestPackageState(packageId, workflowStart.WorkflowInstanceId, "Failed", DateTimeOffset.UtcNow)],
                    []), cancellationToken);
            }
        }

        return new IngestStartResult(
            new IngestStartResponse(startedPackages),
            hadTransferConflict);
    }

    public IngestStatusResponse GetStatus()
    {
        var packages = store.PackageStates
            .GroupBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(packageState => packageState.UpdatedAt).First())
            .OrderBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(packageState => new IngestPackageStatusResponse(
                packageState.PackageId,
                packageState.WorkflowInstanceId,
                packageState.Status,
                packageState.UpdatedAt))
            .ToArray();

        return new IngestStatusResponse(packages);
    }

    private static OutboxMessage CreateLocalTransferMessage(
        PackageWorkflowStart workflowStart,
        string outputPath)
    {
        var payload = JsonSerializer.Serialize(new LocalManifestTransferRequest(
            workflowStart.PackageId,
            workflowStart.PackagePath,
            outputPath));

        return new OutboxMessage(
            MessageId: $"local-transfer-{workflowStart.PackageId}-{Guid.NewGuid():N}",
            Destination: "local.manifest.transfer",
            MessageType: nameof(LocalManifestTransferRequest),
            PayloadJson: payload,
            CorrelationId: workflowStart.CorrelationId,
            CreatedAt: workflowStart.AcceptedAt);
    }
}
