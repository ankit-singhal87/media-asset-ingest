namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class Watch
{
    public required string WatchId { get; set; }

    public required string PathToWatch { get; set; }

    public required string Status { get; set; }

    public required string CallbackUrlTemplate { get; set; }

    public required string CallbackPayloadTemplate { get; set; }

    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
