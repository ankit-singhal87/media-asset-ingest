namespace MediaIngest.Observability;

public static class DiagnosticEventNames
{
    public const string Scan = "ingest.scan";
    public const string Readiness = "ingest.readiness";
    public const string Copy = "ingest.copy";
    public const string OutboxDispatch = "outbox.dispatch";
    public const string CommandStarted = "command.started";
    public const string CommandProgress = "command.progress";
    public const string CommandSucceeded = "command.succeeded";
    public const string CommandFailed = "command.failed";
    public const string Success = "ingest.succeeded";
    public const string Failure = "ingest.failed";

    public static IReadOnlyList<string> All { get; } =
    [
        Scan,
        Readiness,
        Copy,
        OutboxDispatch,
        CommandStarted,
        CommandProgress,
        CommandSucceeded,
        CommandFailed,
        Success,
        Failure
    ];
}
