namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class WatchEvent
{
    public required string EventId { get; set; }

    public required string WatchId { get; set; }

    public required string EventType { get; set; }

    public bool IsFile { get; set; }

    public required string TargetEventSourcePath { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public required string CallbackUrl { get; set; }

    public required string CallbackPayloadJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
