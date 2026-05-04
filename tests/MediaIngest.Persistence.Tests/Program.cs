using System.Data;
using System.Data.Common;
using MediaIngest.Persistence;

var store = new InMemoryIngestPersistenceStore();

var packageState = new IngestPackageState(
    PackageId: "package-001",
    WorkflowInstanceId: "workflow-package-001",
    Status: "WorkAccepted",
    UpdatedAt: new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));

var command = new OutboxMessage(
    MessageId: "message-001",
    Destination: "media-ingest.video",
    MessageType: "ProcessVideo",
    PayloadJson: """{"packageId":"package-001","filePath":"video/source.mov"}""",
    CorrelationId: "correlation-001",
    CreatedAt: packageState.UpdatedAt);

await store.SaveAsync(new PersistenceBatch([packageState], [command]));

AssertEqual(1, store.PackageStates.Count, "saved business state count");
AssertEqual(1, store.OutboxMessages.Count, "saved outbox message count");
AssertEqual("package-001", store.PackageStates[0].PackageId, "business state package id");
AssertEqual("message-001", store.OutboxMessages[0].MessageId, "outbox message id");

var updatedPackageState = packageState with
{
    Status = "WorkDispatched",
    UpdatedAt = packageState.UpdatedAt.AddMinutes(5)
};

await store.SaveAsync(new PersistenceBatch([updatedPackageState], []));

var savedPackageState = await store.GetPackageStateAsync("package-001");
var missingPackageState = await store.GetPackageStateAsync("missing-package");

AssertEqual(1, store.PackageStates.Count, "package state upsert count");
AssertEqual("WorkDispatched", savedPackageState?.Status, "queried package state status");
AssertEqual(updatedPackageState.UpdatedAt, savedPackageState?.UpdatedAt, "queried package state updated at");
AssertEqual(null, missingPackageState, "missing package state");

var timelineRecord = new BusinessTimelineRecord(
    EventId: "timeline-001",
    WorkflowInstanceId: "workflow-package-001",
    NodeId: "scan-package",
    PackageId: "package-001",
    CorrelationId: "correlation-001",
    OccurredAt: packageState.UpdatedAt.AddSeconds(1),
    Status: "Succeeded",
    Message: "Package scan persisted discovered file count.");

var logRecord = new NodeDiagnosticLogRecord(
    LogId: "log-001",
    WorkflowInstanceId: "workflow-package-001",
    NodeId: "scan-package",
    PackageId: "package-001",
    CorrelationId: "correlation-001",
    OccurredAt: packageState.UpdatedAt.AddSeconds(2),
    Level: "Information",
    Message: "Scan node loaded manifest metadata.",
    TraceId: "trace-001",
    SpanId: "span-001");

await store.SaveAsync(new PersistenceBatch([], [command], [timelineRecord], [logRecord]));

AssertEqual(1, store.OutboxMessages.Count, "duplicate outbox message id is idempotent");
AssertEqual("message-001", store.OutboxMessages[0].MessageId, "idempotent outbox message id");
AssertEqual(1, store.TimelineRecords.Count, "saved timeline record count");
AssertEqual(1, store.NodeDiagnosticLogs.Count, "saved node diagnostic log count");

var savedTimeline = await store.GetWorkflowNodeTimelineAsync("workflow-package-001", "scan-package");
var savedLogs = await store.GetWorkflowNodeLogsAsync("workflow-package-001", "scan-package");

AssertEqual("Package scan persisted discovered file count.", savedTimeline.Single().Message, "queried timeline message");
AssertEqual("Scan node loaded manifest metadata.", savedLogs.Single().Message, "queried log message");
AssertEqual("trace-001", savedLogs.Single().TraceId, "queried log trace id");

var missingTimeline = await store.GetWorkflowNodeTimelineAsync("workflow-package-001", "missing-node");
var missingLogs = await store.GetWorkflowNodeLogsAsync("missing-workflow", "scan-package");

AssertEqual(0, missingTimeline.Count, "missing node timeline count");
AssertEqual(0, missingLogs.Count, "missing workflow log count");

var stateConfidenceStore = new InMemoryIngestPersistenceStore();
var laterPendingMessage = command with
{
    MessageId = "message-later",
    CreatedAt = packageState.UpdatedAt.AddMinutes(10)
};
var earlierPendingMessage = command with
{
    MessageId = "message-earlier",
    CreatedAt = packageState.UpdatedAt.AddMinutes(1)
};
var sameTimePendingMessage = command with
{
    MessageId = "message-same-time",
    CreatedAt = packageState.UpdatedAt.AddMinutes(1)
};
var dispatchedMessage = command with
{
    MessageId = "message-dispatched",
    CreatedAt = packageState.UpdatedAt.AddMinutes(2),
    DispatchedAt = packageState.UpdatedAt.AddMinutes(3)
};

await stateConfidenceStore.SaveAsync(new PersistenceBatch(
    [],
    [laterPendingMessage, sameTimePendingMessage, dispatchedMessage, earlierPendingMessage]));

var pendingMessages = await stateConfidenceStore.GetPendingOutboxMessagesAsync();

AssertSequenceEqual(
    ["message-earlier", "message-same-time", "message-later"],
    pendingMessages.Select(message => message.MessageId).ToArray(),
    "pending outbox message order");

var claimedMessages = await stateConfidenceStore.ClaimPendingOutboxMessagesAsync(
    packageState.UpdatedAt.AddMinutes(20),
    packageState.UpdatedAt.AddMinutes(25));

AssertSequenceEqual(
    ["message-earlier", "message-same-time", "message-later"],
    claimedMessages.Select(message => message.MessageId).ToArray(),
    "claimed outbox message order");
AssertTrue(
    claimedMessages.All(message => message.DispatchClaimExpiresAt == packageState.UpdatedAt.AddMinutes(25)),
    "claimed outbox message expiry");

var activeClaimMessages = await stateConfidenceStore.ClaimPendingOutboxMessagesAsync(
    packageState.UpdatedAt.AddMinutes(21),
    packageState.UpdatedAt.AddMinutes(26));

AssertEqual(0, activeClaimMessages.Count, "active outbox claim prevents reclaim");

var expiredClaimMessages = await stateConfidenceStore.ClaimPendingOutboxMessagesAsync(
    packageState.UpdatedAt.AddMinutes(30),
    packageState.UpdatedAt.AddMinutes(35));

AssertEqual(3, expiredClaimMessages.Count, "expired outbox claim can be reclaimed");

await stateConfidenceStore.MarkOutboxMessageDispatchedAsync("message-earlier", packageState.UpdatedAt.AddMinutes(40));

pendingMessages = await stateConfidenceStore.GetPendingOutboxMessagesAsync();

AssertSequenceEqual(
    ["message-same-time", "message-later"],
    pendingMessages.Select(message => message.MessageId).ToArray(),
    "pending outbox message order after dispatch");
AssertEqual(
    null,
    stateConfidenceStore.OutboxMessages.Single(message => message.MessageId == "message-earlier").DispatchClaimExpiresAt,
    "dispatched outbox message claim cleared");

var timelineConfidenceStore = new InMemoryIngestPersistenceStore();
var timelineLater = timelineRecord with
{
    EventId = "timeline-later",
    OccurredAt = packageState.UpdatedAt.AddMinutes(4),
    Message = "Later node event."
};
var timelineEarlier = timelineRecord with
{
    EventId = "timeline-earlier",
    OccurredAt = packageState.UpdatedAt.AddMinutes(3),
    Message = "Earlier node event."
};
var timelineSameTime = timelineRecord with
{
    EventId = "timeline-same-time",
    OccurredAt = packageState.UpdatedAt.AddMinutes(3),
    Message = "Same-time node event."
};
var timelineOtherNode = timelineRecord with
{
    EventId = "timeline-other-node",
    NodeId = "classify-files",
    OccurredAt = packageState.UpdatedAt.AddMinutes(2)
};

await timelineConfidenceStore.SaveAsync(new PersistenceBatch(
    [],
    [],
    [timelineLater, timelineSameTime, timelineOtherNode, timelineEarlier],
    []));

var orderedTimeline = await timelineConfidenceStore.GetWorkflowNodeTimelineAsync("workflow-package-001", "scan-package");

AssertSequenceEqual(
    ["timeline-earlier", "timeline-same-time", "timeline-later"],
    orderedTimeline.Select(record => record.EventId).ToArray(),
    "workflow node timeline order");

var logConfidenceStore = new InMemoryIngestPersistenceStore();
var logLater = logRecord with
{
    LogId = "log-later",
    OccurredAt = packageState.UpdatedAt.AddMinutes(7),
    Message = "Later log."
};
var logEarlier = logRecord with
{
    LogId = "log-earlier",
    OccurredAt = packageState.UpdatedAt.AddMinutes(6),
    Message = "Earlier log."
};
var logSameTime = logRecord with
{
    LogId = "log-same-time",
    OccurredAt = packageState.UpdatedAt.AddMinutes(6),
    Message = "Same-time log."
};
var logOtherWorkflow = logRecord with
{
    LogId = "log-other-workflow",
    WorkflowInstanceId = "workflow-other",
    OccurredAt = packageState.UpdatedAt.AddMinutes(5)
};

await logConfidenceStore.SaveAsync(new PersistenceBatch(
    [],
    [],
    [],
    [logLater, logSameTime, logOtherWorkflow, logEarlier]));

var orderedLogs = await logConfidenceStore.GetWorkflowNodeLogsAsync("workflow-package-001", "scan-package");

AssertSequenceEqual(
    ["log-earlier", "log-same-time", "log-later"],
    orderedLogs.Select(record => record.LogId).ToArray(),
    "workflow node diagnostic log order");

var rejected = false;

try
{
    await store.SaveAsync(new PersistenceBatch(
        [packageState with { PackageId = "package-002" }],
        [command with { MessageId = "" }],
        [timelineRecord with { EventId = "timeline-002" }],
        [logRecord with { LogId = "log-002" }]));
}
catch (ArgumentException)
{
    rejected = true;
}

AssertTrue(rejected, "invalid outbox message rejects the persistence batch");
AssertEqual(1, store.PackageStates.Count, "business state count after rejected batch");
AssertEqual("package-001", store.PackageStates[0].PackageId, "business state after rejected batch");
AssertEqual(1, store.OutboxMessages.Count, "outbox count after rejected batch");
AssertEqual(1, store.TimelineRecords.Count, "timeline count after rejected batch");
AssertEqual(1, store.NodeDiagnosticLogs.Count, "diagnostic log count after rejected batch");

AssertContains("CREATE TABLE IF NOT EXISTS ingest_package_states", PostgresIngestSchema.SchemaSql, "package state table DDL");
AssertContains("CREATE TABLE IF NOT EXISTS outbox_messages", PostgresIngestSchema.SchemaSql, "outbox table DDL");
AssertContains("CREATE TABLE IF NOT EXISTS business_timeline_records", PostgresIngestSchema.SchemaSql, "timeline table DDL");
AssertContains("CREATE TABLE IF NOT EXISTS node_diagnostic_logs", PostgresIngestSchema.SchemaSql, "diagnostic log table DDL");
AssertContains("CREATE INDEX IF NOT EXISTS idx_outbox_messages_pending", PostgresIngestSchema.SchemaSql, "pending outbox index DDL");

var recordingConnection = new RecordingDbConnection();
var postgresStore = new PostgresIngestPersistenceStore(_ => ValueTask.FromResult<DbConnection>(recordingConnection));

await postgresStore.SaveAsync(new PersistenceBatch([packageState], [command], [timelineRecord], [logRecord]));

AssertEqual(1, recordingConnection.BeginTransactionCount, "postgres save transaction count");
AssertTrue(recordingConnection.Committed, "postgres save commits transaction");
AssertEqual(4, recordingConnection.ExecutedCommands.Count, "postgres save command count");
AssertContains("INSERT INTO ingest_package_states", recordingConnection.ExecutedCommands[0].CommandText, "postgres package upsert command");
AssertContains("INSERT INTO outbox_messages", recordingConnection.ExecutedCommands[1].CommandText, "postgres outbox insert command");
AssertContains("INSERT INTO business_timeline_records", recordingConnection.ExecutedCommands[2].CommandText, "postgres timeline insert command");
AssertContains("INSERT INTO node_diagnostic_logs", recordingConnection.ExecutedCommands[3].CommandText, "postgres diagnostic log insert command");
AssertContains("ON CONFLICT (message_id) DO NOTHING", recordingConnection.ExecutedCommands[1].CommandText, "postgres outbox insert idempotency");
AssertEqual("package-001", recordingConnection.ExecutedCommands[0].Parameters["@package_id"], "postgres package id parameter");
AssertEqual("message-001", recordingConnection.ExecutedCommands[1].Parameters["@message_id"], "postgres message id parameter");
AssertEqual("timeline-001", recordingConnection.ExecutedCommands[2].Parameters["@event_id"], "postgres timeline event id parameter");
AssertEqual("log-001", recordingConnection.ExecutedCommands[3].Parameters["@log_id"], "postgres diagnostic log id parameter");

var packageStateReadConnection = new RecordingDbConnection();
var packageStateReadStore = new PostgresIngestPersistenceStore(_ => ValueTask.FromResult<DbConnection>(packageStateReadConnection));

await packageStateReadStore.GetPackageStateAsync("package-001");

AssertEqual(1, packageStateReadConnection.ExecutedCommands.Count, "postgres package state read command count");
AssertContains("FROM ingest_package_states", packageStateReadConnection.ExecutedCommands[0].CommandText, "postgres package state read table");
AssertContains("WHERE package_id = @package_id", packageStateReadConnection.ExecutedCommands[0].CommandText, "postgres package state read filter");
AssertEqual("package-001", packageStateReadConnection.ExecutedCommands[0].Parameters["@package_id"], "postgres package state read parameter");

var schemaConnection = new RecordingDbConnection();
var schemaStore = new PostgresIngestPersistenceStore(_ => ValueTask.FromResult<DbConnection>(schemaConnection));

await schemaStore.CreateSchemaAsync();

AssertEqual(1, schemaConnection.ExecutedCommands.Count, "postgres schema command count");
AssertEqual(PostgresIngestSchema.SchemaSql, schemaConnection.ExecutedCommands[0].CommandText, "postgres schema command text");

Console.WriteLine("MediaIngest persistence boundary smoke tests passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true.");
    }
}

static void AssertContains(string expected, string actual, string name)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{name}: expected to contain '{expected}'.");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"{name}: expected '{string.Join(", ", expected)}', got '{string.Join(", ", actual)}'.");
    }
}

#pragma warning disable CS8764, CS8765
sealed class RecordingDbConnection : DbConnection
{
    private ConnectionState state = ConnectionState.Closed;

    public int BeginTransactionCount { get; private set; }

    public bool Committed { get; private set; }

    public List<ExecutedCommand> ExecutedCommands { get; } = [];

    public override string? ConnectionString { get; set; } = string.Empty;

    public override string Database => "media_ingest_test";

    public override string DataSource => "recording";

    public override string ServerVersion => "16";

    public override ConnectionState State => state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => state = ConnectionState.Closed;

    public override void Open() => state = ConnectionState.Open;

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        state = ConnectionState.Open;
        return Task.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        BeginTransactionCount++;
        return new RecordingDbTransaction(this, isolationLevel, () => Committed = true);
    }

    protected override DbCommand CreateDbCommand() => new RecordingDbCommand(this);
}

sealed class RecordingDbTransaction(
    DbConnection connection,
    IsolationLevel isolationLevel,
    Action onCommit) : DbTransaction
{
    public override IsolationLevel IsolationLevel { get; } = isolationLevel;

    protected override DbConnection DbConnection { get; } = connection;

    public override void Commit() => onCommit();

    public override Task CommitAsync(CancellationToken cancellationToken = default)
    {
        onCommit();
        return Task.CompletedTask;
    }

    public override void Rollback()
    {
    }
}

sealed class RecordingDbCommand(RecordingDbConnection connection) : DbCommand
{
    private readonly RecordingParameterCollection parameters = new();

    public override string? CommandText { get; set; } = string.Empty;

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; } = connection;

    protected override DbParameterCollection DbParameterCollection => parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        Record();
        return 1;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        Record();
        return Task.FromResult(1);
    }

    public override object? ExecuteScalar() => throw new NotSupportedException();

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new RecordingDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        Record();
        return new EmptyRecordingDataReader();
    }

    private void Record() => connection.ExecutedCommands.Add(new ExecutedCommand(
        CommandText ?? string.Empty,
        parameters.Cast<RecordingDbParameter>().ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value)));
}

sealed class RecordingDbParameter : DbParameter
{
    public override DbType DbType { get; set; }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

    public override string ParameterName { get; set; } = string.Empty;

    public override string? SourceColumn { get; set; } = string.Empty;

    public override object? Value { get; set; }

    public override bool SourceColumnNullMapping { get; set; }

    public override int Size { get; set; }

    public override void ResetDbType()
    {
    }
}

sealed class RecordingParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> parameters = [];

    public override int Count => parameters.Count;

    public override object SyncRoot => this;

    public override int Add(object value)
    {
        parameters.Add((DbParameter)value);
        return parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value);
        }
    }

    public override void Clear() => parameters.Clear();

    public override bool Contains(object value) => parameters.Contains((DbParameter)value);

    public override bool Contains(string value) => parameters.Any(parameter => parameter.ParameterName == value);

    public override void CopyTo(Array array, int index) => parameters.ToArray().CopyTo(array, index);

    public override IEnumerator<object> GetEnumerator() => parameters.Cast<object>().GetEnumerator();

    public override int IndexOf(object value) => parameters.IndexOf((DbParameter)value);

    public override int IndexOf(string parameterName) => parameters.FindIndex(parameter => parameter.ParameterName == parameterName);

    public override void Insert(int index, object value) => parameters.Insert(index, (DbParameter)value);

    public override void Remove(object value) => parameters.Remove((DbParameter)value);

    public override void RemoveAt(int index) => parameters.RemoveAt(index);

    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    protected override DbParameter GetParameter(int index) => parameters[index];

    protected override DbParameter GetParameter(string parameterName) => parameters[IndexOf(parameterName)];

    protected override void SetParameter(int index, DbParameter value) => parameters[index] = value;

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            parameters[index] = value;
        }
        else
        {
            parameters.Add(value);
        }
    }
}

sealed record ExecutedCommand(string CommandText, IReadOnlyDictionary<string, object?> Parameters);

sealed class EmptyRecordingDataReader : DbDataReader
{
    public override int Depth => 0;

    public override int FieldCount => 0;

    public override bool HasRows => false;

    public override bool IsClosed => false;

    public override int RecordsAffected => 0;

    public override object this[int ordinal] => throw new IndexOutOfRangeException();

    public override object this[string name] => throw new IndexOutOfRangeException();

    public override bool GetBoolean(int ordinal) => throw new IndexOutOfRangeException();

    public override byte GetByte(int ordinal) => throw new IndexOutOfRangeException();

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        throw new IndexOutOfRangeException();

    public override char GetChar(int ordinal) => throw new IndexOutOfRangeException();

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        throw new IndexOutOfRangeException();

    public override string GetDataTypeName(int ordinal) => throw new IndexOutOfRangeException();

    public override DateTime GetDateTime(int ordinal) => throw new IndexOutOfRangeException();

    public override decimal GetDecimal(int ordinal) => throw new IndexOutOfRangeException();

    public override double GetDouble(int ordinal) => throw new IndexOutOfRangeException();

    public override Type GetFieldType(int ordinal) => throw new IndexOutOfRangeException();

    public override float GetFloat(int ordinal) => throw new IndexOutOfRangeException();

    public override Guid GetGuid(int ordinal) => throw new IndexOutOfRangeException();

    public override short GetInt16(int ordinal) => throw new IndexOutOfRangeException();

    public override int GetInt32(int ordinal) => throw new IndexOutOfRangeException();

    public override long GetInt64(int ordinal) => throw new IndexOutOfRangeException();

    public override string GetName(int ordinal) => throw new IndexOutOfRangeException();

    public override int GetOrdinal(string name) => throw new IndexOutOfRangeException();

    public override string GetString(int ordinal) => throw new IndexOutOfRangeException();

    public override object GetValue(int ordinal) => throw new IndexOutOfRangeException();

    public override int GetValues(object[] values) => 0;

    public override bool IsDBNull(int ordinal) => throw new IndexOutOfRangeException();

    public override bool NextResult() => false;

    public override bool Read() => false;

    public override IEnumerator<object> GetEnumerator() => Enumerable.Empty<object>().GetEnumerator();
}
#pragma warning restore CS8764, CS8765
