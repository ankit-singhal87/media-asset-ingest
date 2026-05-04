namespace MediaIngest.Persistence;

public sealed record BusinessTimelineRecord(
    string EventId,
    string WorkflowInstanceId,
    string NodeId,
    string PackageId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string Status,
    string Message);
