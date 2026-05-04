namespace MediaIngest.Observability;

public static class DiagnosticEventNames
{
    public const string Scan = "ingest.scan";
    public const string Readiness = "ingest.readiness";
    public const string Copy = "ingest.copy";
    public const string OutboxDispatch = "outbox.dispatch";
    public const string CommandAccepted = "command.accepted";
    public const string CommandRejected = "command.rejected";
    public const string CommandDuplicateSkipped = "command.duplicate_skipped";
    public const string CommandStarted = "command.started";
    public const string CommandProgress = "command.progress";
    public const string CommandSucceeded = "command.succeeded";
    public const string CommandFailed = "command.failed";
    public const string CommandTimedOut = "command.timed_out";
    public const string Success = "ingest.succeeded";
    public const string Failure = "ingest.failed";

    public static IReadOnlyList<string> All { get; } =
    [
        Scan,
        Readiness,
        Copy,
        OutboxDispatch,
        CommandAccepted,
        CommandRejected,
        CommandDuplicateSkipped,
        CommandStarted,
        CommandProgress,
        CommandSucceeded,
        CommandFailed,
        CommandTimedOut,
        Success,
        Failure
    ];
}
