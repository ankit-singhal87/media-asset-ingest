namespace MediaIngest.Observability;

public sealed record ObservabilityCorrelationContext
{
    public ObservabilityCorrelationContext(
        string WorkflowInstanceId,
        string PackageId,
        string FileId,
        string WorkItemId,
        string NodeId,
        string AgentType,
        string QueueName,
        string CorrelationId,
        string CausationId,
        string TraceId,
        string SpanId)
    {
        this.WorkflowInstanceId = Require(WorkflowInstanceId, nameof(WorkflowInstanceId));
        this.PackageId = Require(PackageId, nameof(PackageId));
        this.FileId = Require(FileId, nameof(FileId));
        this.WorkItemId = Require(WorkItemId, nameof(WorkItemId));
        this.NodeId = Require(NodeId, nameof(NodeId));
        this.AgentType = Require(AgentType, nameof(AgentType));
        this.QueueName = Require(QueueName, nameof(QueueName));
        this.CorrelationId = Require(CorrelationId, nameof(CorrelationId));
        this.CausationId = Require(CausationId, nameof(CausationId));
        this.TraceId = Require(TraceId, nameof(TraceId));
        this.SpanId = Require(SpanId, nameof(SpanId));
    }

    public string WorkflowInstanceId { get; }
    public string PackageId { get; }
    public string FileId { get; }
    public string WorkItemId { get; }
    public string NodeId { get; }
    public string AgentType { get; }
    public string QueueName { get; }
    public string CorrelationId { get; }
    public string CausationId { get; }
    public string TraceId { get; }
    public string SpanId { get; }

    public static ObservabilityCorrelationContext Create(
        string workflowInstanceId,
        string packageId,
        string fileId,
        string workItemId,
        string nodeId,
        string agentType,
        string queueName,
        string correlationId,
        string causationId,
        string traceId,
        string spanId)
    {
        return new ObservabilityCorrelationContext(
            workflowInstanceId,
            packageId,
            fileId,
            workItemId,
            nodeId,
            agentType,
            queueName,
            correlationId,
            causationId,
            traceId,
            spanId);
    }

    public IReadOnlyDictionary<string, string> ToFields()
    {
        return new Dictionary<string, string>
        {
            [CorrelationFieldNames.WorkflowInstanceId] = WorkflowInstanceId,
            [CorrelationFieldNames.PackageId] = PackageId,
            [CorrelationFieldNames.FileId] = FileId,
            [CorrelationFieldNames.WorkItemId] = WorkItemId,
            [CorrelationFieldNames.NodeId] = NodeId,
            [CorrelationFieldNames.AgentType] = AgentType,
            [CorrelationFieldNames.QueueName] = QueueName,
            [CorrelationFieldNames.CorrelationId] = CorrelationId,
            [CorrelationFieldNames.CausationId] = CausationId,
            [CorrelationFieldNames.TraceId] = TraceId,
            [CorrelationFieldNames.SpanId] = SpanId
        };
    }

    private static string Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value;
    }
}
