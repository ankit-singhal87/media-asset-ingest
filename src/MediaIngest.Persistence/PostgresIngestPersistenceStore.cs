using System.Data;
using System.Data.Common;
using Npgsql;

namespace MediaIngest.Persistence;

public sealed class PostgresIngestPersistenceStore(
    Func<CancellationToken, ValueTask<DbConnection>> openConnection) : IIngestPersistenceStore
{
    public static PostgresIngestPersistenceStore Create(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new PostgresIngestPersistenceStore(
            _ => ValueTask.FromResult<DbConnection>(new NpgsqlConnection(connectionString)));
    }

    public async Task CreateSchemaAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await ExecuteNonQueryAsync(
            connection,
            transaction: null,
            PostgresIngestSchema.SchemaSql,
            [],
            cancellationToken);
    }

    public async Task SaveAsync(PersistenceBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        cancellationToken.ThrowIfCancellationRequested();

        IngestPersistenceBatchValidator.Validate(batch);

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        foreach (var packageState in batch.PackageStates)
        {
            await ExecuteNonQueryAsync(
                connection,
                transaction,
                """
                INSERT INTO ingest_package_states (
                    package_id,
                    workflow_instance_id,
                    status,
                    updated_at
                )
                VALUES (
                    @package_id,
                    @workflow_instance_id,
                    @status,
                    @updated_at
                )
                ON CONFLICT (package_id) DO UPDATE SET
                    workflow_instance_id = EXCLUDED.workflow_instance_id,
                    status = EXCLUDED.status,
                    updated_at = EXCLUDED.updated_at;
                """,
                [
                    ("@package_id", packageState.PackageId),
                    ("@workflow_instance_id", packageState.WorkflowInstanceId),
                    ("@status", packageState.Status),
                    ("@updated_at", packageState.UpdatedAt)
                ],
                cancellationToken);
        }

        foreach (var outboxMessage in batch.OutboxMessages)
        {
            await ExecuteNonQueryAsync(
                connection,
                transaction,
                """
                INSERT INTO outbox_messages (
                    message_id,
                    destination,
                    message_type,
                    payload_json,
                    correlation_id,
                    created_at,
                    dispatched_at,
                    dispatch_claim_expires_at
                )
                VALUES (
                    @message_id,
                    @destination,
                    @message_type,
                    @payload_json::jsonb,
                    @correlation_id,
                    @created_at,
                    @dispatched_at,
                    @dispatch_claim_expires_at
                )
                ON CONFLICT (message_id) DO NOTHING;
                """,
                [
                    ("@message_id", outboxMessage.MessageId),
                    ("@destination", outboxMessage.Destination),
                    ("@message_type", outboxMessage.MessageType),
                    ("@payload_json", outboxMessage.PayloadJson),
                    ("@correlation_id", outboxMessage.CorrelationId),
                    ("@created_at", outboxMessage.CreatedAt),
                    ("@dispatched_at", outboxMessage.DispatchedAt),
                    ("@dispatch_claim_expires_at", outboxMessage.DispatchClaimExpiresAt)
                ],
                cancellationToken);
        }

        foreach (var timelineRecord in batch.TimelineRecords)
        {
            await ExecuteNonQueryAsync(
                connection,
                transaction,
                """
                INSERT INTO business_timeline_records (
                    event_id,
                    workflow_instance_id,
                    node_id,
                    package_id,
                    correlation_id,
                    occurred_at,
                    status,
                    message
                )
                VALUES (
                    @event_id,
                    @workflow_instance_id,
                    @node_id,
                    @package_id,
                    @correlation_id,
                    @occurred_at,
                    @status,
                    @message
                )
                ON CONFLICT (event_id) DO NOTHING;
                """,
                [
                    ("@event_id", timelineRecord.EventId),
                    ("@workflow_instance_id", timelineRecord.WorkflowInstanceId),
                    ("@node_id", timelineRecord.NodeId),
                    ("@package_id", timelineRecord.PackageId),
                    ("@correlation_id", timelineRecord.CorrelationId),
                    ("@occurred_at", timelineRecord.OccurredAt),
                    ("@status", timelineRecord.Status),
                    ("@message", timelineRecord.Message)
                ],
                cancellationToken);
        }

        foreach (var nodeDiagnosticLog in batch.NodeDiagnosticLogs)
        {
            await ExecuteNonQueryAsync(
                connection,
                transaction,
                """
                INSERT INTO node_diagnostic_logs (
                    log_id,
                    workflow_instance_id,
                    node_id,
                    package_id,
                    correlation_id,
                    occurred_at,
                    level,
                    message,
                    trace_id,
                    span_id
                )
                VALUES (
                    @log_id,
                    @workflow_instance_id,
                    @node_id,
                    @package_id,
                    @correlation_id,
                    @occurred_at,
                    @level,
                    @message,
                    @trace_id,
                    @span_id
                )
                ON CONFLICT (log_id) DO NOTHING;
                """,
                [
                    ("@log_id", nodeDiagnosticLog.LogId),
                    ("@workflow_instance_id", nodeDiagnosticLog.WorkflowInstanceId),
                    ("@node_id", nodeDiagnosticLog.NodeId),
                    ("@package_id", nodeDiagnosticLog.PackageId),
                    ("@correlation_id", nodeDiagnosticLog.CorrelationId),
                    ("@occurred_at", nodeDiagnosticLog.OccurredAt),
                    ("@level", nodeDiagnosticLog.Level),
                    ("@message", nodeDiagnosticLog.Message),
                    ("@trace_id", nodeDiagnosticLog.TraceId),
                    ("@span_id", nodeDiagnosticLog.SpanId)
                ],
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IngestPackageState?> GetPackageStateAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package id is required.", nameof(packageId));
        }

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                package_id,
                workflow_instance_id,
                status,
                updated_at
            FROM ingest_package_states
            WHERE package_id = @package_id;
            """;
        AddParameter(command, "@package_id", packageId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new IngestPackageState(
            PackageId: reader.GetString(0),
            WorkflowInstanceId: reader.GetString(1),
            Status: reader.GetString(2),
            UpdatedAt: ReadDateTimeOffset(reader, 3));
    }

    public async Task<IReadOnlyList<IngestPackageState>> ListPackageStatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                package_id,
                workflow_instance_id,
                status,
                updated_at
            FROM ingest_package_states
            ORDER BY package_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var states = new List<IngestPackageState>();

        while (await reader.ReadAsync(cancellationToken))
        {
            states.Add(new IngestPackageState(
                PackageId: reader.GetString(0),
                WorkflowInstanceId: reader.GetString(1),
                Status: reader.GetString(2),
                UpdatedAt: ReadDateTimeOffset(reader, 3)));
        }

        return states;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ListOutboxMessagesAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation id is required.", nameof(correlationId));
        }

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                message_id,
                destination,
                message_type,
                payload_json,
                correlation_id,
                created_at,
                dispatched_at,
                dispatch_claim_expires_at
            FROM outbox_messages
            WHERE correlation_id = @correlation_id
            ORDER BY created_at, message_id;
            """;
        AddParameter(command, "@correlation_id", correlationId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await ReadOutboxMessagesAsync(reader, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                message_id,
                destination,
                message_type,
                payload_json,
                correlation_id,
                created_at,
                dispatched_at,
                dispatch_claim_expires_at
            FROM outbox_messages
            WHERE dispatched_at IS NULL
            ORDER BY created_at, message_id;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await ReadOutboxMessagesAsync(reader, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingOutboxMessagesAsync(
        DateTimeOffset claimedAt,
        DateTimeOffset claimExpiresAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (claimExpiresAt <= claimedAt)
        {
            throw new ArgumentException("Outbox message claim expiry must be after the claim time.", nameof(claimExpiresAt));
        }

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH claimable_messages AS (
                SELECT message_id
                FROM outbox_messages
                WHERE dispatched_at IS NULL
                  AND (
                      dispatch_claim_expires_at IS NULL OR
                      dispatch_claim_expires_at <= @claimed_at
                  )
                ORDER BY created_at, message_id
                FOR UPDATE SKIP LOCKED
            )
            UPDATE outbox_messages AS outbox_message
            SET dispatch_claim_expires_at = @claim_expires_at
            FROM claimable_messages
            WHERE outbox_message.message_id = claimable_messages.message_id
            RETURNING
                outbox_message.message_id,
                outbox_message.destination,
                outbox_message.message_type,
                outbox_message.payload_json,
                outbox_message.correlation_id,
                outbox_message.created_at,
                outbox_message.dispatched_at,
                outbox_message.dispatch_claim_expires_at;
            """;

        AddParameter(command, "@claimed_at", claimedAt);
        AddParameter(command, "@claim_expires_at", claimExpiresAt);

        IReadOnlyList<OutboxMessage> messages;

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            messages = await ReadOutboxMessagesAsync(reader, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return messages;
    }

    public async Task MarkOutboxMessageDispatchedAsync(
        string messageId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Outbox message id is required.", nameof(messageId));
        }

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        var affectedRows = await ExecuteNonQueryAsync(
            connection,
            transaction: null,
            """
            UPDATE outbox_messages
            SET
                dispatched_at = COALESCE(dispatched_at, @dispatched_at),
                dispatch_claim_expires_at = NULL
            WHERE message_id = @message_id;
            """,
            [
                ("@dispatched_at", dispatchedAt),
                ("@message_id", messageId)
            ],
            cancellationToken);

        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"Outbox message '{messageId}' was not found.");
        }
    }

    public async Task<IReadOnlyList<BusinessTimelineRecord>> GetWorkflowNodeTimelineAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                event_id,
                workflow_instance_id,
                node_id,
                package_id,
                correlation_id,
                occurred_at,
                status,
                message
            FROM business_timeline_records
            WHERE workflow_instance_id = @workflow_instance_id
              AND node_id = @node_id
            ORDER BY occurred_at, event_id;
            """;
        AddParameter(command, "@workflow_instance_id", workflowInstanceId);
        AddParameter(command, "@node_id", nodeId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var records = new List<BusinessTimelineRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new BusinessTimelineRecord(
                EventId: reader.GetString(0),
                WorkflowInstanceId: reader.GetString(1),
                NodeId: reader.GetString(2),
                PackageId: reader.GetString(3),
                CorrelationId: reader.GetString(4),
                OccurredAt: ReadDateTimeOffset(reader, 5),
                Status: reader.GetString(6),
                Message: reader.GetString(7)));
        }

        return records;
    }

    public async Task<IReadOnlyList<NodeDiagnosticLogRecord>> GetWorkflowNodeLogsAsync(
        string workflowInstanceId,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = await openConnection(cancellationToken);
        await OpenIfNeededAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                log_id,
                workflow_instance_id,
                node_id,
                package_id,
                correlation_id,
                occurred_at,
                level,
                message,
                trace_id,
                span_id
            FROM node_diagnostic_logs
            WHERE workflow_instance_id = @workflow_instance_id
              AND node_id = @node_id
            ORDER BY occurred_at, log_id;
            """;
        AddParameter(command, "@workflow_instance_id", workflowInstanceId);
        AddParameter(command, "@node_id", nodeId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var records = new List<NodeDiagnosticLogRecord>();

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new NodeDiagnosticLogRecord(
                LogId: reader.GetString(0),
                WorkflowInstanceId: reader.GetString(1),
                NodeId: reader.GetString(2),
                PackageId: reader.GetString(3),
                CorrelationId: reader.GetString(4),
                OccurredAt: ReadDateTimeOffset(reader, 5),
                Level: reader.GetString(6),
                Message: reader.GetString(7),
                TraceId: reader.IsDBNull(8) ? null : reader.GetString(8),
                SpanId: reader.IsDBNull(9) ? null : reader.GetString(9)));
        }

        return records;
    }

    private static async Task OpenIfNeededAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static async Task<int> ExecuteNonQueryAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string commandText,
        IReadOnlyList<(string Name, object? Value)> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;

        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static async Task<IReadOnlyList<OutboxMessage>> ReadOutboxMessagesAsync(
        DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var messages = new List<OutboxMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessage(
                MessageId: reader.GetString(0),
                Destination: reader.GetString(1),
                MessageType: reader.GetString(2),
                PayloadJson: reader.GetString(3),
                CorrelationId: reader.GetString(4),
                CreatedAt: ReadDateTimeOffset(reader, 5),
                DispatchedAt: reader.IsDBNull(6) ? null : ReadDateTimeOffset(reader, 6),
                DispatchClaimExpiresAt: reader.IsDBNull(7) ? null : ReadDateTimeOffset(reader, 7)));
        }

        return messages;
    }

    private static DateTimeOffset ReadDateTimeOffset(DbDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);

        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException($"Column {ordinal} is not a timestamp.")
        };
    }
}
