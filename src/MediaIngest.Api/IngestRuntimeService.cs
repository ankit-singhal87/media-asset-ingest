using System.Text.Json;
using MediaIngest.Contracts.Workflow;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;

namespace MediaIngest.Api;

public sealed class IngestRuntimeService(
    IngestRuntimePaths paths,
    IngestMountScanner scanner,
    ManifestReadinessGate readinessGate,
    PackageWorkflowStarter workflowStarter,
    InMemoryIngestPersistenceStore store,
    OutboxDispatcher outboxDispatcher) : IAsyncDisposable
{
    private static readonly TimeSpan WatchInterval = TimeSpan.FromMilliseconds(100);

    private readonly object watcherLock = new();
    private readonly HashSet<string> terminalPackageIds = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim scanLock = new(1, 1);
    private CancellationTokenSource? watcherCancellation;
    private Task? watcherTask;

    public async Task<IngestStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.InputPath);
        Directory.CreateDirectory(paths.OutputPath);

        var result = await ScanOnceAsync(cancellationToken);
        StartWatcher();

        return result;
    }

    private void StartWatcher()
    {
        lock (watcherLock)
        {
            if (watcherTask is { IsCompleted: false })
            {
                return;
            }

            watcherCancellation?.Dispose();
            watcherCancellation = new CancellationTokenSource();
            watcherTask = WatchAsync(watcherCancellation.Token);
        }
    }

    private async Task WatchAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ScanOnceAsync(cancellationToken);
                await Task.Delay(WatchInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task<IngestStartResult> ScanOnceAsync(CancellationToken cancellationToken)
    {
        await scanLock.WaitAsync(cancellationToken);

        try
        {
            var candidates = scanner.FindPackageCandidates(paths.InputPath);
            var startedPackages = new List<StartedIngestPackageResponse>();
            var hadTransferConflict = false;

            foreach (var candidate in candidates.Where(readinessGate.IsReady))
            {
                var packageId = Path.GetFileName(candidate.PackagePath);

                if (terminalPackageIds.Contains(packageId))
                {
                    continue;
                }

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
                    terminalPackageIds.Add(packageId);
                }
                catch (LocalManifestTransferConflictException)
                {
                    hadTransferConflict = true;
                    await store.MarkOutboxMessageDispatchedAsync(
                        message.MessageId,
                        DateTimeOffset.UtcNow,
                        cancellationToken);
                    await store.SaveAsync(new PersistenceBatch(
                        [new IngestPackageState(packageId, workflowStart.WorkflowInstanceId, "Failed", DateTimeOffset.UtcNow)],
                        []), cancellationToken);
                    terminalPackageIds.Add(packageId);
                }
            }

            return new IngestStartResult(
                new IngestStartResponse(startedPackages),
                hadTransferConflict);
        }
        finally
        {
            scanLock.Release();
        }
    }

    public IngestStatusResponse GetStatus()
    {
        var packages = store.PackageStates
            .GroupBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(packageState => new IngestPackageStatusResponse(
                packageState.PackageId,
                packageState.WorkflowInstanceId,
                packageState.Status,
                packageState.UpdatedAt))
            .ToArray();

        return new IngestStatusResponse(packages);
    }

    public WorkflowGraphDto? GetWorkflowGraph(string workflowInstanceId)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
        {
            return null;
        }

        var packageState = store.PackageStates
            .GroupBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .SingleOrDefault(packageState =>
                string.Equals(packageState.WorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal));

        return packageState is null
            ? null
            : PackageWorkflowGraphProjection.FromPackageStatus(
                packageState.PackageId,
                packageState.WorkflowInstanceId,
                packageState.Status);
    }

    public WorkflowNodeDetailsDto? GetWorkflowNodeDetails(string workflowInstanceId, string nodeId)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId) || string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        var graph = GetWorkflowGraph(workflowInstanceId);
        var node = graph?.Nodes.SingleOrDefault(candidate =>
            string.Equals(candidate.NodeId, nodeId, StringComparison.Ordinal));

        if (graph is null || node is null)
        {
            return null;
        }

        var packageState = store.PackageStates
            .Where(packageState =>
                string.Equals(packageState.WorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal))
            .OrderBy(packageState => packageState.UpdatedAt)
            .LastOrDefault();

        if (packageState is null)
        {
            return null;
        }

        var correlationId = $"correlation-{graph.PackageId}";
        var occurredAt = packageState.UpdatedAt;

        return new WorkflowNodeDetailsDto(
            WorkflowInstanceId: graph.WorkflowInstanceId,
            NodeId: node.NodeId,
            Timeline:
            [
                new WorkflowTimelineEntryDto(
                    OccurredAt: occurredAt,
                    Status: node.Status,
                    Message: $"{node.DisplayName} is {node.Status.ToString().ToLowerInvariant()}",
                    CorrelationId: correlationId)
            ],
            Logs:
            [
                new WorkflowNodeLogEntryDto(
                    OccurredAt: occurredAt,
                    Level: node.Status == WorkflowNodeStatus.Failed ? "Error" : "Information",
                    Message: $"{node.DisplayName} projected from in-memory workflow state.",
                    CorrelationId: correlationId,
                    TraceId: null,
                    SpanId: null)
            ]);
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource? cancellation;
        Task? task;

        lock (watcherLock)
        {
            cancellation = watcherCancellation;
            task = watcherTask;
            watcherCancellation = null;
            watcherTask = null;
        }

        if (cancellation is null)
        {
            scanLock.Dispose();
            return;
        }

        await cancellation.CancelAsync();

        if (task is not null)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        cancellation.Dispose();
        scanLock.Dispose();
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
