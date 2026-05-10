namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed record WatchDefinition(
    string WatchId,
    string PathToWatch,
    string CallbackUrlTemplate,
    string CallbackPayloadTemplate);

internal sealed record CallbackTemplateValues(
    string EventType,
    bool IsFile,
    string TargetEventSourcePath,
    DateTimeOffset Timestamp);

internal sealed record ObservedFileSystemEvent(
    string WatchId,
    string EventType,
    bool IsFile,
    string TargetEventSourcePath,
    DateTimeOffset OccurredAt);
