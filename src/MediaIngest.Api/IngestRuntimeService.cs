using System.Text.Json;
using MediaIngest.Contracts.Commands;
using MediaIngest.Contracts.Workflow;
using MediaIngest.Essence.Classification;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;
using MediaIngest.Worker.Watcher;
using MediaIngest.Workflow;
using MediaIngest.Workflow.Orchestrator;

namespace MediaIngest.Api;

public sealed class IngestRuntimeService(
    IngestRuntimePaths paths,
    IngestMountScanner scanner,
    ManifestReadinessGate readinessGate,
    PackageWorkflowStarter workflowStarter,
    WorkflowGraphProjector workflowGraphProjector,
    IIngestPersistenceStore store,
    OutboxDispatcher outboxDispatcher) : IAsyncDisposable
{
    private static readonly TimeSpan WatchInterval = TimeSpan.FromMilliseconds(100);
    private readonly DoneMarkerReadinessGate doneMarkerGate = new();

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
                var latestState = await store.GetPackageStateAsync(packageId, cancellationToken);

                if (terminalPackageIds.Contains(packageId) || IsTerminal(latestState?.Status))
                {
                    terminalPackageIds.Add(packageId);
                    continue;
                }

                var workflowStart = CreateWorkflowStart(packageId, candidate.PackagePath);

                if (latestState is null)
                {
                    var message = CreateLocalTransferMessage(workflowStart, paths.OutputPath);
                    var mediaCommandMessages = CreateMediaCommandMessages(
                        workflowStart,
                        scanner.FindPackageFiles(candidate));
                    var timelineRecords = new List<BusinessTimelineRecord>
                    {
                        CreateTimelineRecord(
                            workflowStart,
                            "package-start",
                            "Started",
                            $"Package ingest started for {packageId}.",
                            workflowStart.AcceptedAt),
                        CreateTimelineRecord(
                            workflowStart,
                            "dispatch-processing",
                            "Running",
                            $"Dispatched {mediaCommandMessages.Count} processing commands for {packageId}.",
                            workflowStart.AcceptedAt)
                    };
                    var logRecords = new List<NodeDiagnosticLogRecord>
                    {
                        CreateLogRecord(
                            workflowStart,
                            "package-start",
                            "Information",
                            $"Package start persisted business state for {packageId}.",
                            workflowStart.AcceptedAt),
                        CreateLogRecord(
                            workflowStart,
                            "dispatch-processing",
                            "Information",
                            $"Dispatch node persisted {mediaCommandMessages.Count} outbox commands for {packageId}.",
                            workflowStart.AcceptedAt)
                    };

                    if (doneMarkerGate.IsDone(candidate))
                    {
                        var discoveredFileCount = scanner.FindPackageFiles(candidate)
                            .Count(file => !string.Equals(file.PackageRelativePath, "done.marker", StringComparison.Ordinal));

                        timelineRecords.Add(CreateTimelineRecord(
                            workflowStart,
                            "scan-package",
                            "Succeeded",
                            $"Package scan found {discoveredFileCount} files for {packageId}.",
                            workflowStart.AcceptedAt));
                        logRecords.Add(CreateLogRecord(
                            workflowStart,
                            "scan-package",
                            "Information",
                            $"Scan node persisted package file discovery for {packageId}.",
                            workflowStart.AcceptedAt));
                    }

                    await store.SaveAsync(new PersistenceBatch(
                        [new IngestPackageState(packageId, workflowStart.WorkflowInstanceId, "Started", workflowStart.AcceptedAt)],
                        [message, .. mediaCommandMessages],
                        timelineRecords,
                        logRecords), cancellationToken);

                    startedPackages.Add(new StartedIngestPackageResponse(
                        packageId,
                        workflowStart.WorkflowInstanceId));
                }

                hadTransferConflict |= await DispatchAndMaybeCompleteAsync(
                    candidate,
                    workflowStart,
                    cancellationToken);
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

    private PackageWorkflowStart CreateWorkflowStart(string packageId, string packagePath)
    {
        return workflowStarter.Start(new PackageIngestRequest(
            PackageId: packageId,
            PackagePath: packagePath,
            CorrelationId: $"correlation-{packageId}",
            AcceptedAt: DateTimeOffset.UtcNow));
    }

    private async Task<bool> DispatchAndMaybeCompleteAsync(
        IngestPackageCandidate candidate,
        PackageWorkflowStart workflowStart,
        CancellationToken cancellationToken)
    {
        try
        {
            await outboxDispatcher.DispatchPendingAsync(cancellationToken);

            if (!doneMarkerGate.IsDone(candidate))
            {
                return false;
            }

            var mediaCommandMessages = CreateMediaCommandMessages(
                workflowStart,
                scanner.FindPackageFiles(candidate));
            var reconciledAt = DateTimeOffset.UtcNow;
            var discoveredFileCount = scanner.FindPackageFiles(candidate)
                .Count(file => !string.Equals(file.PackageRelativePath, "done.marker", StringComparison.Ordinal));

            await store.SaveAsync(new PersistenceBatch(
                [],
                mediaCommandMessages,
                [
                    CreateTimelineRecord(
                        workflowStart,
                        "scan-package",
                        "Succeeded",
                        $"Package scan found {discoveredFileCount} files for {workflowStart.PackageId}.",
                        reconciledAt),
                    CreateTimelineRecord(
                        workflowStart,
                        "reconcile-package",
                        "Succeeded",
                        $"done.marker reconciled package {workflowStart.PackageId}.",
                        reconciledAt),
                    CreateTimelineRecord(
                        workflowStart,
                        "dispatch-processing",
                        "Succeeded",
                        $"Dispatched {mediaCommandMessages.Count} processing commands for {workflowStart.PackageId}.",
                        reconciledAt)
                ],
                [
                    CreateLogRecord(
                        workflowStart,
                        "scan-package",
                        "Information",
                        $"Scan node persisted package file discovery for {workflowStart.PackageId}.",
                        reconciledAt),
                    CreateLogRecord(
                        workflowStart,
                        "reconcile-package",
                        "Information",
                        $"Reconcile node processed done.marker for {workflowStart.PackageId}.",
                        reconciledAt),
                    CreateLogRecord(
                        workflowStart,
                        "dispatch-processing",
                        "Information",
                        $"Dispatch node persisted {mediaCommandMessages.Count} outbox commands for {workflowStart.PackageId}.",
                        reconciledAt)
                ]), cancellationToken);
            await outboxDispatcher.DispatchPendingAsync(cancellationToken);

            var completedAt = DateTimeOffset.UtcNow;
            await store.SaveAsync(new PersistenceBatch(
                [new IngestPackageState(workflowStart.PackageId, workflowStart.WorkflowInstanceId, "Succeeded", completedAt)],
                [],
                [
                    CreateTimelineRecord(
                        workflowStart,
                        "finalize-package",
                        "Succeeded",
                        $"Package {workflowStart.PackageId} completed successfully.",
                        completedAt)
                ],
                [
                    CreateLogRecord(
                        workflowStart,
                        "finalize-package",
                        "Information",
                        $"Finalize node persisted success for {workflowStart.PackageId}.",
                        completedAt)
                ]), cancellationToken);
            terminalPackageIds.Add(workflowStart.PackageId);

            return false;
        }
        catch (LocalManifestTransferConflictException)
        {
            var messageId = await GetPendingLocalTransferMessageIdAsync(
                workflowStart.PackageId,
                cancellationToken);

            if (messageId is null)
            {
                throw;
            }

            await store.MarkOutboxMessageDispatchedAsync(
                messageId,
                DateTimeOffset.UtcNow,
                cancellationToken);
            var failedAt = DateTimeOffset.UtcNow;
            await store.SaveAsync(new PersistenceBatch(
                [new IngestPackageState(workflowStart.PackageId, workflowStart.WorkflowInstanceId, "Failed", failedAt)],
                [],
                [
                    CreateTimelineRecord(
                        workflowStart,
                        "finalize-package",
                        "Failed",
                        $"Package {workflowStart.PackageId} failed during local manifest transfer.",
                        failedAt)
                ],
                [
                    CreateLogRecord(
                        workflowStart,
                        "finalize-package",
                        "Error",
                        $"Finalize node recorded local manifest transfer conflict for {workflowStart.PackageId}.",
                        failedAt)
                ]), cancellationToken);
            terminalPackageIds.Add(workflowStart.PackageId);

            return true;
        }
    }

    private async Task<string?> GetPendingLocalTransferMessageIdAsync(
        string packageId,
        CancellationToken cancellationToken)
    {
        var messages = await store.ListOutboxMessagesAsync($"correlation-{packageId}", cancellationToken);

        return messages
            .Where(message =>
                message.MessageType == nameof(LocalManifestTransferRequest)
                && string.Equals(message.CorrelationId, $"correlation-{packageId}", StringComparison.Ordinal)
                && string.Equals(message.Destination, "local.manifest.transfer", StringComparison.Ordinal)
                && message.DispatchedAt is null)
            .OrderBy(message => message.CreatedAt)
            .Select(message => message.MessageId)
            .FirstOrDefault();
    }

    private static bool IsTerminal(string? status)
    {
        return string.Equals(status, "Succeeded", StringComparison.Ordinal)
            || string.Equals(status, "Failed", StringComparison.Ordinal);
    }

    public async Task<IngestStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var packageStates = await store.ListPackageStatesAsync(cancellationToken);
        var packages = packageStates
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

    public IngestStatusResponse GetStatus()
    {
        return GetStatusAsync().GetAwaiter().GetResult();
    }

    public async Task<WorkflowListResponse> ListWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var packageStates = await store.ListPackageStatesAsync(cancellationToken);
        var workflows = packageStates
            .GroupBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(packageState => new WorkflowListItemResponse(
                packageState.WorkflowInstanceId,
                packageState.PackageId,
                packageState.Status,
                packageState.UpdatedAt))
            .ToArray();

        return new WorkflowListResponse(workflows);
    }

    public WorkflowListResponse ListWorkflows()
    {
        return ListWorkflowsAsync().GetAwaiter().GetResult();
    }

    public async Task<WorkflowGraphDto?> GetWorkflowGraphAsync(
        string workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
        {
            return null;
        }

        var packageStates = await store.ListPackageStatesAsync(cancellationToken);
        var packageState = packageStates
            .GroupBy(packageState => packageState.PackageId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .SingleOrDefault(packageState =>
                string.Equals(packageState.WorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal));

        return packageState is null
            ? null
            : workflowGraphProjector.ProjectInstance(new WorkflowGraphProjectionRequest(
                WorkflowName: WorkflowContractNames.PackageIngestWorkflow,
                WorkflowInstanceId: packageState.WorkflowInstanceId,
                PackageId: packageState.PackageId,
                Status: MapPackageStatus(packageState.Status),
                ParentWorkflowInstanceId: null,
                CommandWorkItems: (await GetMediaCommandWorkItemsAsync(
                    packageState.PackageId,
                    cancellationToken)).ToArray()));
    }

    public WorkflowGraphDto? GetWorkflowGraph(string workflowInstanceId)
    {
        return GetWorkflowGraphAsync(workflowInstanceId).GetAwaiter().GetResult();
    }

    public async Task<WorkflowNodeDetailsDto?> GetWorkflowNodeDetailsAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId) || string.IsNullOrWhiteSpace(nodeId))
        {
            return null;
        }

        var graph = await GetWorkflowGraphAsync(workflowInstanceId, cancellationToken);
        var node = graph?.Nodes.SingleOrDefault(candidate =>
            string.Equals(candidate.NodeId, nodeId, StringComparison.Ordinal));

        if (graph is null || node is null)
        {
            return null;
        }

        var timelineRecords = await store.GetWorkflowNodeTimelineAsync(
            workflowInstanceId,
            nodeId,
            cancellationToken);
        var timeline = timelineRecords
            .Select(record => new WorkflowTimelineEntryDto(
                OccurredAt: record.OccurredAt,
                Status: MapTimelineStatus(record.Status),
                Message: record.Message,
                CorrelationId: record.CorrelationId))
            .ToArray();

        var logRecords = await store.GetWorkflowNodeLogsAsync(
            workflowInstanceId,
            nodeId,
            cancellationToken);
        var logs = logRecords
            .Select(record => new WorkflowNodeLogEntryDto(
                OccurredAt: record.OccurredAt,
                Level: record.Level,
                Message: record.Message,
                CorrelationId: record.CorrelationId,
                TraceId: record.TraceId,
                SpanId: record.SpanId))
            .ToArray();

        return new WorkflowNodeDetailsDto(
            WorkflowInstanceId: graph.WorkflowInstanceId,
            NodeId: node.NodeId,
            Timeline: timeline,
            Logs: logs);
    }

    public WorkflowNodeDetailsDto? GetWorkflowNodeDetails(string workflowInstanceId, string nodeId)
    {
        return GetWorkflowNodeDetailsAsync(workflowInstanceId, nodeId).GetAwaiter().GetResult();
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

    private static IReadOnlyList<OutboxMessage> CreateMediaCommandMessages(
        PackageWorkflowStart workflowStart,
        IReadOnlyList<IngestPackageFile> discoveredFiles)
    {
        return discoveredFiles
            .Where(file => !IsPackageMetadataFile(file.PackageRelativePath))
            .Select(file => CreateMediaCommandMessage(workflowStart, file))
            .ToArray();
    }

    private static OutboxMessage CreateMediaCommandMessage(
        PackageWorkflowStart workflowStart,
        IngestPackageFile file)
    {
        var commandName = SelectCommandName(file);
        var route = CommandRoutingPolicy.Route(commandName, file.FileSizeBytes);
        var command = new MediaCommandEnvelope(
            CommandId: $"command-{workflowStart.PackageId}-{SanitizeIdentifier(file.PackageRelativePath)}",
            CommandName: commandName,
            TopicName: route.TopicName,
            ExecutionClass: route.ExecutionClass,
            CommandLine: CreateLocalCommandLine(commandName, file.PackageRelativePath),
            WorkingDirectory: workflowStart.PackagePath,
            InputPaths: [file.FilePath],
            OutputPaths: [],
            CorrelationId: workflowStart.CorrelationId);

        return new OutboxMessage(
            MessageId: command.CommandId,
            Destination: route.TopicName,
            MessageType: nameof(MediaCommandEnvelope),
            PayloadJson: JsonSerializer.Serialize(command),
            CorrelationId: workflowStart.CorrelationId,
            CreatedAt: workflowStart.AcceptedAt);
    }

    private static string SelectCommandName(IngestPackageFile file)
    {
        return EssenceClassifier.Classify(file.PackageRelativePath) switch
        {
            EssenceType.VideoSource => CommandNames.CreateProxy,
            EssenceType.Audio => CommandNames.CreateProxy,
            EssenceType.Text => CommandNames.RunSecurityScan,
            _ => CommandNames.CreateChecksum
        };
    }

    private static string CreateLocalCommandLine(string commandName, string packageRelativePath)
    {
        return $"{commandName} {packageRelativePath}";
    }

    private async Task<IReadOnlyList<WorkflowCommandWorkItem>> GetMediaCommandWorkItemsAsync(
        string packageId,
        CancellationToken cancellationToken)
    {
        var correlationId = $"correlation-{packageId}";
        var messages = await store.ListOutboxMessagesAsync(correlationId, cancellationToken);

        return messages
            .Where(message =>
                message.MessageType == nameof(MediaCommandEnvelope)
                && string.Equals(message.CorrelationId, correlationId, StringComparison.Ordinal))
            .Select(message => JsonSerializer.Deserialize<MediaCommandEnvelope>(message.PayloadJson))
            .Where(command => command is not null)
            .Cast<MediaCommandEnvelope>()
            .OrderBy(command => command.InputPaths.SingleOrDefault() ?? command.CommandId, StringComparer.Ordinal)
            .Select(command => new WorkflowCommandWorkItem(
                NodeId: $"command-{SanitizeIdentifier(GetPackageRelativeCommandPath(command, packageId))}",
                DisplayName: CreateCommandDisplayName(command),
                WorkItemId: command.CommandId))
            .ToArray();
    }

    private static WorkflowNodeStatus MapPackageStatus(string status)
    {
        return status switch
        {
            "Started" => WorkflowNodeStatus.Running,
            "Succeeded" => WorkflowNodeStatus.Succeeded,
            "Failed" => WorkflowNodeStatus.Failed,
            _ => WorkflowNodeStatus.Pending
        };
    }

    private static string CreateCommandDisplayName(MediaCommandEnvelope command)
    {
        var inputPath = command.InputPaths.SingleOrDefault() ?? command.CommandId;
        return $"{command.CommandName} {Path.GetFileName(inputPath)}";
    }

    private static string GetPackageRelativeCommandPath(MediaCommandEnvelope command, string packageId)
    {
        var inputPath = command.InputPaths.SingleOrDefault() ?? command.CommandId;
        var packagePathMarker = $"{Path.DirectorySeparatorChar}{packageId}{Path.DirectorySeparatorChar}";
        var markerIndex = inputPath.IndexOf(packagePathMarker, StringComparison.Ordinal);

        return markerIndex < 0
            ? inputPath
            : inputPath[(markerIndex + packagePathMarker.Length)..];
    }

    private static bool IsPackageMetadataFile(string packageRelativePath)
    {
        return string.Equals(packageRelativePath, "manifest.json", StringComparison.Ordinal)
            || string.Equals(packageRelativePath, "manifest.json.checksum", StringComparison.Ordinal)
            || string.Equals(packageRelativePath, "done.marker", StringComparison.Ordinal);
    }

    private static string SanitizeIdentifier(string value)
    {
        var chars = value.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
        return new string(chars).Trim('-').ToLowerInvariant();
    }

    private static BusinessTimelineRecord CreateTimelineRecord(
        PackageWorkflowStart workflowStart,
        string nodeId,
        string status,
        string message,
        DateTimeOffset occurredAt)
    {
        return new BusinessTimelineRecord(
            EventId: $"timeline-{workflowStart.WorkflowInstanceId}-{nodeId}-{SanitizeIdentifier(status)}-{occurredAt.ToUnixTimeMilliseconds()}",
            WorkflowInstanceId: workflowStart.WorkflowInstanceId,
            NodeId: nodeId,
            PackageId: workflowStart.PackageId,
            CorrelationId: workflowStart.CorrelationId,
            OccurredAt: occurredAt,
            Status: status,
            Message: message);
    }

    private static NodeDiagnosticLogRecord CreateLogRecord(
        PackageWorkflowStart workflowStart,
        string nodeId,
        string level,
        string message,
        DateTimeOffset occurredAt)
    {
        return new NodeDiagnosticLogRecord(
            LogId: $"log-{workflowStart.WorkflowInstanceId}-{nodeId}-{SanitizeIdentifier(level)}-{occurredAt.ToUnixTimeMilliseconds()}",
            WorkflowInstanceId: workflowStart.WorkflowInstanceId,
            NodeId: nodeId,
            PackageId: workflowStart.PackageId,
            CorrelationId: workflowStart.CorrelationId,
            OccurredAt: occurredAt,
            Level: level,
            Message: message,
            TraceId: null,
            SpanId: null);
    }

    private static WorkflowNodeStatus MapTimelineStatus(string status)
    {
        return Enum.TryParse<WorkflowNodeStatus>(status, ignoreCase: true, out var nodeStatus)
            ? nodeStatus
            : MapPackageStatus(status);
    }
}
