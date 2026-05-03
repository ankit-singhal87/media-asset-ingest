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
    }
}
