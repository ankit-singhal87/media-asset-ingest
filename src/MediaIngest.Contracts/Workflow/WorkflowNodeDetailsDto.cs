namespace MediaIngest.Contracts.Workflow;

public sealed record WorkflowNodeDetailsDto(
    string WorkflowInstanceId,
    string NodeId,
    IReadOnlyList<WorkflowTimelineEntryDto> Timeline,
    IReadOnlyList<WorkflowNodeLogEntryDto> Logs);
