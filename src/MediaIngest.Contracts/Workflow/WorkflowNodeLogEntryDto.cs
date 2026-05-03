namespace MediaIngest.Contracts.Workflow;

public sealed record WorkflowNodeLogEntryDto(
    DateTimeOffset OccurredAt,
    string Level,
    string Message,
    string CorrelationId,
    string? TraceId,
    string? SpanId);
