namespace MediaIngest.Observability;

public static class CorrelationFieldNames
{
    public const string WorkflowInstanceId = "workflowInstanceId";
    public const string PackageId = "packageId";
    public const string FileId = "fileId";
    public const string WorkItemId = "workItemId";
    public const string NodeId = "nodeId";
    public const string AgentType = "agentType";
    public const string QueueName = "queueName";
    public const string CorrelationId = "correlationId";
    public const string CausationId = "causationId";
    public const string TraceId = "traceId";
    public const string SpanId = "spanId";

    public static IReadOnlyList<string> All { get; } =
    [
        WorkflowInstanceId,
        PackageId,
        FileId,
        WorkItemId,
        NodeId,
        AgentType,
        QueueName,
        CorrelationId,
        CausationId,
        TraceId,
        SpanId
    ];
}
