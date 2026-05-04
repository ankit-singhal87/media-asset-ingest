namespace MediaIngest.Persistence;

public sealed record NodeDiagnosticLogRecord(
    string LogId,
    string WorkflowInstanceId,
    string NodeId,
    string PackageId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string Level,
    string Message,
    string? TraceId,
    string? SpanId);
