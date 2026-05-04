namespace MediaIngest.Persistence;

internal static class IngestPersistenceBatchValidator
{
    public static void Validate(PersistenceBatch batch)
    {
        foreach (var packageState in batch.PackageStates)
        {
            if (string.IsNullOrWhiteSpace(packageState.PackageId))
            {
                throw new ArgumentException("Package state package id is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(packageState.WorkflowInstanceId))
            {
                throw new ArgumentException("Package state workflow instance id is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(packageState.Status))
            {
                throw new ArgumentException("Package state status is required.", nameof(batch));
            }
        }

        foreach (var outboxMessage in batch.OutboxMessages)
        {
            if (string.IsNullOrWhiteSpace(outboxMessage.MessageId))
            {
                throw new ArgumentException("Outbox message id is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.Destination))
            {
                throw new ArgumentException("Outbox message destination is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.MessageType))
            {
                throw new ArgumentException("Outbox message type is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.PayloadJson))
            {
                throw new ArgumentException("Outbox message payload is required.", nameof(batch));
            }

            if (string.IsNullOrWhiteSpace(outboxMessage.CorrelationId))
            {
                throw new ArgumentException("Outbox message correlation id is required.", nameof(batch));
            }
        }

        foreach (var timelineRecord in batch.TimelineRecords)
        {
            if (string.IsNullOrWhiteSpace(timelineRecord.EventId))
            {
                throw new ArgumentException("Timeline event id is required.", nameof(batch));
            }

            ValidateNodeCorrelation(
                timelineRecord.WorkflowInstanceId,
                timelineRecord.NodeId,
                timelineRecord.PackageId,
                timelineRecord.CorrelationId,
                timelineRecord.Message,
                batch);

            if (string.IsNullOrWhiteSpace(timelineRecord.Status))
            {
                throw new ArgumentException("Timeline status is required.", nameof(batch));
            }
        }

        foreach (var nodeDiagnosticLog in batch.NodeDiagnosticLogs)
        {
            if (string.IsNullOrWhiteSpace(nodeDiagnosticLog.LogId))
            {
                throw new ArgumentException("Node diagnostic log id is required.", nameof(batch));
            }

            ValidateNodeCorrelation(
                nodeDiagnosticLog.WorkflowInstanceId,
                nodeDiagnosticLog.NodeId,
                nodeDiagnosticLog.PackageId,
                nodeDiagnosticLog.CorrelationId,
                nodeDiagnosticLog.Message,
                batch);

            if (string.IsNullOrWhiteSpace(nodeDiagnosticLog.Level))
            {
                throw new ArgumentException("Node diagnostic log level is required.", nameof(batch));
            }
        }
    }

    private static void ValidateNodeCorrelation(
        string workflowInstanceId,
        string nodeId,
        string packageId,
        string correlationId,
        string message,
        PersistenceBatch batch)
    {
        if (string.IsNullOrWhiteSpace(workflowInstanceId))
        {
            throw new ArgumentException("Workflow instance id is required.", nameof(batch));
        }

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node id is required.", nameof(batch));
        }

        if (string.IsNullOrWhiteSpace(packageId))
        {
            throw new ArgumentException("Package id is required.", nameof(batch));
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new ArgumentException("Correlation id is required.", nameof(batch));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Node diagnostic message is required.", nameof(batch));
        }
    }
}
