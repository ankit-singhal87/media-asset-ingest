namespace MediaIngest.Contracts.Workflow;

public enum WorkflowNodeKind
{
    WorkflowStep,
    Activity,
    ChildWorkflow,
    WorkItem
}
