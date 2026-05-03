namespace MediaIngest.Contracts.Workflow;

public sealed record WorkflowTimelineEntryDto(
    DateTimeOffset OccurredAt,
    WorkflowNodeStatus Status,
    string Message,
    string CorrelationId);
