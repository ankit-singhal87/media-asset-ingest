namespace MediaIngest.Contracts.Workflow;

public enum WorkflowNodeStatus
{
    Pending,
    Queued,
    Running,
    Succeeded,
    Failed,
    Waiting,
    Skipped,
    Cancelled
}
