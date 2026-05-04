namespace MediaIngest.Persistence;

public sealed class InMemoryIngestPersistenceStore : IIngestPersistenceStore
{
    private readonly object storeLock = new();
    private readonly List<IngestPackageState> packageStates = [];
    private readonly List<OutboxMessage> outboxMessages = [];
    private readonly List<BusinessTimelineRecord> timelineRecords = [];
    private readonly List<NodeDiagnosticLogRecord> nodeDiagnosticLogs = [];

    public IReadOnlyList<IngestPackageState> PackageStates
    {
        get
        {
            lock (storeLock)
            {
                return packageStates.ToArray();
            }
        }
    }

    public IReadOnlyList<OutboxMessage> OutboxMessages
    {
        get
        {
            lock (storeLock)
            {
                return outboxMessages.ToArray();
            }
        }
    }

    public IReadOnlyList<BusinessTimelineRecord> TimelineRecords
    {
        get
        {
            lock (storeLock)
            {
                return timelineRecords.ToArray();
            }
        }
    }

    public IReadOnlyList<NodeDiagnosticLogRecord> NodeDiagnosticLogs
    {
        get
        {
            lock (storeLock)
            {
                return nodeDiagnosticLogs.ToArray();
            }
        }
    }

    public Task SaveAsync(PersistenceBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        cancellationToken.ThrowIfCancellationRequested();

        IngestPersistenceBatchValidator.Validate(batch);

        lock (storeLock)
        {
            foreach (var packageState in batch.PackageStates)
            {
                var packageIndex = packageStates.FindIndex(existing => existing.PackageId == packageState.PackageId);

                if (packageIndex >= 0)
                {
                    packageStates[packageIndex] = packageState;
                }
                else
                {
                    packageStates.Add(packageState);
                }
            }

            foreach (var outboxMessage in batch.OutboxMessages)
            {
                if (!outboxMessages.Any(existing => existing.MessageId == outboxMessage.MessageId))
                {
                    outboxMessages.Add(outboxMessage);
                }
            }

            foreach (var timelineRecord in batch.TimelineRecords)
            {
                if (!timelineRecords.Any(existing => existing.EventId == timelineRecord.EventId))
                {
                    timelineRecords.Add(timelineRecord);
                }
            }

            foreach (var nodeDiagnosticLog in batch.NodeDiagnosticLogs)
            {
                if (!nodeDiagnosticLogs.Any(existing => existing.LogId == nodeDiagnosticLog.LogId))
                {
                    nodeDiagnosticLogs.Add(nodeDiagnosticLog);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<IngestPackageState?> GetPackageStateAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package id is required.", nameof(packageId));
        }

        IngestPackageState? packageState;

        lock (storeLock)
        {
            packageState = packageStates.SingleOrDefault(
                state => string.Equals(state.PackageId, packageId, StringComparison.Ordinal));
        }

        return Task.FromResult(packageState);
    }

    public Task<IReadOnlyList<BusinessTimelineRecord>> GetWorkflowNodeTimelineAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<BusinessTimelineRecord> records;

        lock (storeLock)
        {
            records = timelineRecords
                .Where(record =>
                    string.Equals(record.WorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal) &&
                    string.Equals(record.NodeId, nodeId, StringComparison.Ordinal))
                .OrderBy(record => record.OccurredAt)
                .ThenBy(record => record.EventId, StringComparer.Ordinal)
                .ToArray();
        }

        return Task.FromResult(records);
    }

    public Task<IReadOnlyList<NodeDiagnosticLogRecord>> GetWorkflowNodeLogsAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<NodeDiagnosticLogRecord> records;

        lock (storeLock)
        {
            records = nodeDiagnosticLogs
                .Where(record =>
                    string.Equals(record.WorkflowInstanceId, workflowInstanceId, StringComparison.Ordinal) &&
                    string.Equals(record.NodeId, nodeId, StringComparison.Ordinal))
                .OrderBy(record => record.OccurredAt)
                .ThenBy(record => record.LogId, StringComparer.Ordinal)
                .ToArray();
        }

        return Task.FromResult(records);
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<OutboxMessage> pendingMessages;

        lock (storeLock)
        {
            pendingMessages = outboxMessages
                .Where(message => message.DispatchedAt is null)
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.MessageId, StringComparer.Ordinal)
                .ToArray();
        }

        return Task.FromResult(pendingMessages);
    }

    public Task<IReadOnlyList<OutboxMessage>> ClaimPendingOutboxMessagesAsync(
        DateTimeOffset claimedAt,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (claimExpiresAt <= claimedAt)
        {
            throw new ArgumentException("Outbox message claim expiry must be after the claim time.", nameof(claimExpiresAt));
        }

        IReadOnlyList<OutboxMessage> claimedMessages;

        lock (storeLock)
        {
            var claimableMessages = outboxMessages
                .Select((Message, Index) => (Message, Index))
                .Where(candidate =>
                    candidate.Message.DispatchedAt is null &&
                    (candidate.Message.DispatchClaimExpiresAt is null ||
                     candidate.Message.DispatchClaimExpiresAt <= claimedAt))
                .OrderBy(candidate => candidate.Message.CreatedAt)
                .ThenBy(candidate => candidate.Message.MessageId)
                .ToArray();

            claimedMessages = claimableMessages
                .Select(candidate => candidate.Message with { DispatchClaimExpiresAt = claimExpiresAt })
                .ToArray();

            foreach (var claimedMessage in claimedMessages)
            {
                var messageIndex = outboxMessages.FindIndex(message => message.MessageId == claimedMessage.MessageId);
                outboxMessages[messageIndex] = claimedMessage;
            }
        }

        return Task.FromResult(claimedMessages);
    }

    public Task MarkOutboxMessageDispatchedAsync(
        string messageId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Outbox message id is required.", nameof(messageId));
        }

        lock (storeLock)
        {
            var messageIndex = outboxMessages.FindIndex(message => message.MessageId == messageId);

            if (messageIndex < 0)
            {
                throw new InvalidOperationException($"Outbox message '{messageId}' was not found.");
            }

            if (outboxMessages[messageIndex].DispatchedAt is null)
            {
                outboxMessages[messageIndex] = outboxMessages[messageIndex] with
                {
                    DispatchedAt = dispatchedAt,
                    DispatchClaimExpiresAt = null
                };
            }
        }

        return Task.CompletedTask;
    }

}
