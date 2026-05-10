namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class CallbackOutboxMessage
{
    public required string MessageId { get; set; }

    public required string EventId { get; set; }

    public required string Destination { get; set; }

    public required string MessageType { get; set; }

    public required string PayloadJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DispatchedAt { get; set; }

    public DateTimeOffset? DispatchClaimExpiresAt { get; set; }
}
