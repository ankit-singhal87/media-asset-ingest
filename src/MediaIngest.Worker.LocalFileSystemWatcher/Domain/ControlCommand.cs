namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class ControlCommand
{
    public required string CommandId { get; set; }

    public required string WatchId { get; set; }

    public required string CommandType { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset? AppliedAt { get; set; }

    public required string Result { get; set; }
}
