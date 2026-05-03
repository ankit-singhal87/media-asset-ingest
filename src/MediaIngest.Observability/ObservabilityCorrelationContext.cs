namespace MediaIngest.Observability;

public sealed record ObservabilityCorrelationContext(
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
}
