namespace MediaIngest.Persistence;

public sealed record OutboxMessage(
    string MessageId,
    string Destination,
    string MessageType,
    string PayloadJson,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispatchedAt = null);
