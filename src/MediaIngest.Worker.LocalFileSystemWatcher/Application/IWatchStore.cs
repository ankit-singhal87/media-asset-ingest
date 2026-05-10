namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal interface IWatchStore
{
    Task<ControlCommand?> FindControlCommandAsync(
        string commandId,
        CancellationToken cancellationToken = default);

    Task<Watch?> FindWatchAsync(
        string watchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Watch>> ListDesiredWatchesAsync(CancellationToken cancellationToken = default);

    void AddWatch(Watch watch);

    void AddControlCommand(ControlCommand command);

    Task SaveWatchEventWithOutboxAsync(
        WatchEvent watchEvent,
        CallbackOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

internal interface IEventRecorder
{
    Task<WatchEvent> RecordAsync(
        ObservedFileSystemEvent observedEvent,
        CancellationToken cancellationToken = default);
}
