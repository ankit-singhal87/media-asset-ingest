using System.Text.Json;
using MediaIngest.Contracts.Commands;
using MediaIngest.Contracts.Workflow;
using MediaIngest.Essence.Classification;
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
                var latestState = GetLatestPackageState(packageId);

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

                    await store.SaveAsync(new PersistenceBatch(
                        [new IngestPackageState(packageId, workflowStart.WorkflowInstanceId, "Started", workflowStart.AcceptedAt)],
                        [message, .. mediaCommandMessages]), cancellationToken);

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

            await store.SaveAsync(new PersistenceBatch(
                [],
                mediaCommandMessages), cancellationToken);
            await outboxDispatcher.DispatchPendingAsync(cancellationToken);

            await store.SaveAsync(new PersistenceBatch(
                [new IngestPackageState(workflowStart.PackageId, workflowStart.WorkflowInstanceId, "Succeeded", DateTimeOffset.UtcNow)],
                []), cancellationToken);
            terminalPackageIds.Add(workflowStart.PackageId);

            return false;
        }
        catch (LocalManifestTransferConflictException)
        {
            var messageId = GetPendingLocalTransferMessageId(workflowStart.PackageId);

            if (messageId is null)
            {
                throw;
            }

            await store.MarkOutboxMessageDispatchedAsync(
                messageId,
                DateTimeOffset.UtcNow,
                cancellationToken);
            await store.SaveAsync(new PersistenceBatch(
                [new IngestPackageState(workflowStart.PackageId, workflowStart.WorkflowInstanceId, "Failed", DateTimeOffset.UtcNow)],
                []), cancellationToken);
            terminalPackageIds.Add(workflowStart.PackageId);

            return true;
        }
    }

    private string? GetPendingLocalTransferMessageId(string packageId)
    {
        return store.OutboxMessages
            .Where(message =>
                message.MessageType == nameof(LocalManifestTransferRequest)
                && string.Equals(message.CorrelationId, $"correlation-{packageId}", StringComparison.Ordinal)
                && string.Equals(message.Destination, "local.manifest.transfer", StringComparison.Ordinal)
                && message.DispatchedAt is null)
            .OrderBy(message => message.CreatedAt)
            .Select(message => message.MessageId)
            .FirstOrDefault();
    }

    private IngestPackageState? GetLatestPackageState(string packageId)
    {
        return store.PackageStates
            .Where(packageState => string.Equals(packageState.PackageId, packageId, StringComparison.Ordinal))
            .OrderBy(packageState => packageState.UpdatedAt)
            .LastOrDefault();
    }

    private static bool IsTerminal(string? status)
    {
        return string.Equals(status, "Succeeded", StringComparison.Ordinal)
            || string.Equals(status, "Failed", StringComparison.Ordinal);
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
            : CreateWorkflowGraph(
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

    private WorkflowGraphDto CreateWorkflowGraph(
        string packageId,
        string workflowInstanceId,
        string status)
    {
        var workflowStatus = MapPackageStatus(status);
        var nodes = new List<WorkflowNodeDto>
        {
            CreateWorkflowNode("package-start", "Package ingest", WorkflowNodeKind.WorkflowStep, workflowStatus, workflowInstanceId, packageId, null),
            CreateWorkflowNode("scan-package", "Package scan", WorkflowNodeKind.Activity, workflowStatus, workflowInstanceId, packageId, "scan-package"),
            CreateWorkflowNode("classify-files", "Classify discovered files", WorkflowNodeKind.Activity, workflowStatus, workflowInstanceId, packageId, "classify-files"),
            CreateWorkflowNode("dispatch-processing", "Dispatch processing work", WorkflowNodeKind.Activity, workflowStatus, workflowInstanceId, packageId, "dispatch-processing")
        };

        nodes.AddRange(GetMediaCommandEnvelopes(packageId)
            .OrderBy(command => command.InputPaths.SingleOrDefault() ?? command.CommandId, StringComparer.Ordinal)
            .Select(command => CreateWorkflowNode(
                $"command-{SanitizeIdentifier(GetPackageRelativeCommandPath(command, packageId))}",
                CreateCommandDisplayName(command),
                WorkflowNodeKind.WorkItem,
                workflowStatus,
                workflowInstanceId,
                packageId,
                command.CommandId)));

        nodes.Add(CreateWorkflowNode("reconcile-package", "Reconcile package", WorkflowNodeKind.Activity, workflowStatus, workflowInstanceId, packageId, "reconcile-package"));
        nodes.Add(CreateWorkflowNode("finalize-package", "Finalize package", WorkflowNodeKind.Activity, workflowStatus, workflowInstanceId, packageId, "finalize-package"));

        return new WorkflowGraphDto(
            WorkflowInstanceId: workflowInstanceId,
            WorkflowName: WorkflowContractNames.PackageIngestWorkflow,
            PackageId: packageId,
            ParentWorkflowInstanceId: null,
            Nodes: nodes,
            Edges: CreateLinearEdges(nodes));
    }

    private IEnumerable<MediaCommandEnvelope> GetMediaCommandEnvelopes(string packageId)
    {
        var correlationId = $"correlation-{packageId}";

        return store.OutboxMessages
            .Where(message =>
                message.MessageType == nameof(MediaCommandEnvelope)
                && string.Equals(message.CorrelationId, correlationId, StringComparison.Ordinal))
            .Select(message => JsonSerializer.Deserialize<MediaCommandEnvelope>(message.PayloadJson))
            .Where(command => command is not null)
            .Cast<MediaCommandEnvelope>();
    }

    private static WorkflowNodeDto CreateWorkflowNode(
        string nodeId,
        string displayName,
        WorkflowNodeKind kind,
        WorkflowNodeStatus status,
        string workflowInstanceId,
        string packageId,
        string? workItemId)
    {
        return new WorkflowNodeDto(
            NodeId: nodeId,
            DisplayName: displayName,
            Kind: kind,
            Status: status,
            WorkflowInstanceId: workflowInstanceId,
            PackageId: packageId,
            WorkItemId: workItemId,
            ChildWorkflowInstanceId: null);
    }

    private static WorkflowEdgeDto[] CreateLinearEdges(IReadOnlyList<WorkflowNodeDto> nodes)
    {
        return nodes
            .Zip(nodes.Skip(1), (source, target) => new WorkflowEdgeDto(
                EdgeId: $"{source.NodeId}-{target.NodeId}",
                SourceNodeId: source.NodeId,
                TargetNodeId: target.NodeId))
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
}
