using System.Data;
using System.Data.Common;

namespace MediaIngest.Persistence;

public sealed class PostgresIngestPersistenceStore(
    Func<CancellationToken, ValueTask<DbConnection>> openConnection) : IIngestPersistenceStore
{
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
                    dispatched_at
                )
                VALUES (
                    @message_id,
                    @destination,
                    @message_type,
                    @payload_json,
                    @correlation_id,
                    @created_at,
                    @dispatched_at
                );
                """,
                [
                    ("@message_id", outboxMessage.MessageId),
                    ("@destination", outboxMessage.Destination),
                    ("@message_type", outboxMessage.MessageType),
                    ("@payload_json", outboxMessage.PayloadJson),
                    ("@correlation_id", outboxMessage.CorrelationId),
                    ("@created_at", outboxMessage.CreatedAt),
                    ("@dispatched_at", outboxMessage.DispatchedAt)
                ],
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
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
                dispatched_at
            FROM outbox_messages
            WHERE dispatched_at IS NULL
            ORDER BY created_at, message_id;
            """;

        var messages = new List<OutboxMessage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            messages.Add(new OutboxMessage(
                MessageId: reader.GetString(0),
                Destination: reader.GetString(1),
                MessageType: reader.GetString(2),
                PayloadJson: reader.GetString(3),
                CorrelationId: reader.GetString(4),
                CreatedAt: ReadDateTimeOffset(reader, 5),
                DispatchedAt: reader.IsDBNull(6) ? null : ReadDateTimeOffset(reader, 6)));
        }

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
            SET dispatched_at = @dispatched_at
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
