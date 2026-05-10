using Microsoft.EntityFrameworkCore;
using MediaIngest.Worker.LocalFileSystemWatcher;

await VerifyEfModelOwnsTheWatcherSchema();
await VerifyControlCommandsAreIdempotentAndUpdateDesiredState();
VerifyCallbackTemplatesRejectUnsupportedTokens();
VerifyCallbackTemplatesRenderAllowedTokens();
VerifyCallbackPayloadRenderingEscapesJsonTokenValues();
await VerifyEventsAndCallbackOutboxArePersistedTogether();
await VerifySupervisorReconcilesPersistedDesiredState();
await VerifyControlCommandsSignalReconciliation();
await VerifySupervisorPollsForDesiredStateChanges();
await VerifyFileSystemWatcherRuntimeRecordsLocalEvents();

Console.WriteLine("MediaIngest local file system watcher smoke tests passed.");

static async Task VerifyEfModelOwnsTheWatcherSchema()
{
    await using var context = CreateContext();

    AssertEqual(
        "local_file_system_watcher",
        context.Model.GetDefaultSchema(),
        "watcher db context default schema");

    AssertEntityTable<Watch>(context, "watches");
    AssertEntityTable<WatchEvent>(context, "events");
    AssertEntityTable<ControlCommand>(context, "control_commands");
    AssertEntityTable<CallbackOutboxMessage>(context, "outbox_messages");
}

static async Task VerifyControlCommandsAreIdempotentAndUpdateDesiredState()
{
    var now = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
    await using var context = CreateContext();
    var handler = new ControlCommandHandler(context, new CallbackTemplateRenderer(), new FixedTimeProvider(now));
    var definition = new WatchDefinition(
        WatchId: "watch-001",
        PathToWatch: "/mnt/watch/dropbox",
        CallbackUrlTemplate: "https://callback.test/{eventType}",
        CallbackPayloadTemplate: """{"path":"{targetEventSourcePath}","file":"{isFile}","at":"{timestamp}"}""");

    var firstCreate = await handler.HandleAsync("command-create", "create_watcher", definition);
    var duplicateCreate = await handler.HandleAsync("command-create", "create_watcher", definition);
    var suspend = await handler.HandleAsync("command-suspend", "suspend_watcher", definition);
    var resume = await handler.HandleAsync("command-resume", "resume_watcher", definition);

    var watch = await context.Watches.SingleAsync(watch => watch.WatchId == "watch-001");

    AssertEqual("applied", firstCreate.Result, "first create result");
    AssertEqual("duplicate", duplicateCreate.Result, "duplicate create result");
    AssertEqual("applied", suspend.Result, "suspend result");
    AssertEqual("applied", resume.Result, "resume result");
    AssertEqual("active", watch.Status, "watch final status");
    AssertEqual(3, watch.Version, "watch version after create suspend resume");
    AssertEqual(3, await context.ControlCommands.CountAsync(), "idempotent command record count");
}

static void VerifyCallbackTemplatesRejectUnsupportedTokens()
{
    var renderer = new CallbackTemplateRenderer();

    AssertThrows<InvalidOperationException>(
        () => renderer.Validate("https://callback.test/{packageId}", "{}", "invalid url token"),
        "unsupported callback url token");
    AssertThrows<InvalidOperationException>(
        () => renderer.Validate("https://callback.test/{eventType}", """{"bad":"{commandId}"}""", "invalid payload token"),
        "unsupported callback payload token");
}

static void VerifyCallbackTemplatesRenderAllowedTokens()
{
    var renderer = new CallbackTemplateRenderer();
    var occurredAt = new DateTimeOffset(2026, 5, 10, 10, 5, 6, 123, TimeSpan.Zero);
    var values = new CallbackTemplateValues(
        EventType: "created",
        IsFile: true,
        TargetEventSourcePath: "/mnt/watch/dropbox/source.mov",
        Timestamp: occurredAt);

    var url = renderer.Render("https://callback.test/{eventType}/{isFile}", values);
    var payload = renderer.Render(
        """{"path":"{targetEventSourcePath}","timestamp":"{timestamp}"}""",
        values);

    AssertEqual("https://callback.test/created/true", url, "rendered callback url");
    AssertEqual(
        """{"path":"/mnt/watch/dropbox/source.mov","timestamp":"2026-05-10T10:05:06.1230000+00:00"}""",
        payload,
        "rendered callback payload");
}

static void VerifyCallbackPayloadRenderingEscapesJsonTokenValues()
{
    var renderer = new CallbackTemplateRenderer();
    var values = new CallbackTemplateValues(
        EventType: "created",
        IsFile: true,
        TargetEventSourcePath: "/mnt/watch/dropbox/source \"quoted\".mov",
        Timestamp: new DateTimeOffset(2026, 5, 10, 10, 5, 6, TimeSpan.Zero));

    var payload = renderer.RenderPayloadJson("""{"path":"{targetEventSourcePath}"}""", values);

    AssertEqual(
        """{"path":"/mnt/watch/dropbox/source \u0022quoted\u0022.mov"}""",
        payload,
        "rendered callback payload escapes json");
}

static async Task VerifyEventsAndCallbackOutboxArePersistedTogether()
{
    var now = new DateTimeOffset(2026, 5, 10, 11, 0, 0, TimeSpan.Zero);
    await using var context = CreateContext();
    context.Watches.Add(new Watch
    {
        WatchId = "watch-events",
        PathToWatch = "/mnt/watch/dropbox",
        Status = "active",
        CallbackUrlTemplate = "https://callback.test/{eventType}",
        CallbackPayloadTemplate = """{"path":"{targetEventSourcePath}"}""",
        Version = 1,
        CreatedAt = now,
        UpdatedAt = now
    });
    await context.SaveChangesAsync();

    var recorder = new EventRecorder(context, new CallbackTemplateRenderer(), new FixedTimeProvider(now));

    var recorded = await recorder.RecordAsync(new ObservedFileSystemEvent(
        WatchId: "watch-events",
        EventType: "created",
        IsFile: true,
        TargetEventSourcePath: "/mnt/watch/dropbox/source.mov",
        OccurredAt: now.AddSeconds(1)));

    AssertEqual(1, await context.Events.CountAsync(), "persisted filesystem event count");
    AssertEqual(1, await context.OutboxMessages.CountAsync(), "persisted callback outbox count");
    AssertEqual(recorded.EventId, await context.Events.Select(watchEvent => watchEvent.EventId).SingleAsync(), "recorded event id");
    AssertEqual(
        recorded.EventId,
        await context.OutboxMessages.Select(message => message.EventId).SingleAsync(),
        "outbox event id");
    AssertEqual(
        "https://callback.test/created",
        await context.Events.Select(watchEvent => watchEvent.CallbackUrl).SingleAsync(),
        "recorded callback url");
}

static async Task VerifySupervisorReconcilesPersistedDesiredState()
{
    var now = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
    await using var context = CreateContext();
    context.Watches.Add(new Watch
    {
        WatchId = "watch-active",
        PathToWatch = "/mnt/watch/active",
        Status = "active",
        CallbackUrlTemplate = "https://callback.test/{eventType}",
        CallbackPayloadTemplate = "{}",
        Version = 1,
        CreatedAt = now,
        UpdatedAt = now
    });
    await context.SaveChangesAsync();

    var runtime = new RecordingWatcherRuntime();
    var supervisor = new Supervisor(context, runtime);

    await supervisor.ReconcileAsync();

    AssertSequenceEqual(["watch-active"], runtime.StartedWatchIds.ToArray(), "started active watch ids");

    var watch = await context.Watches.SingleAsync(watch => watch.WatchId == "watch-active");
    watch.Status = "suspended";
    watch.Version++;
    await context.SaveChangesAsync();

    await supervisor.ReconcileAsync();

    AssertSequenceEqual(["watch-active"], runtime.StoppedWatchIds.ToArray(), "stopped suspended watch ids");

    watch.Status = "active";
    watch.PathToWatch = "/mnt/watch/active-renamed";
    watch.Version++;
    await context.SaveChangesAsync();

    await supervisor.ReconcileAsync();

    AssertSequenceEqual(["watch-active", "watch-active"], runtime.StartedWatchIds.ToArray(), "restarted changed watch ids");
    AssertEqual("/mnt/watch/active-renamed", runtime.StartedDefinitions[^1].PathToWatch, "restarted watch path");
}

static async Task VerifyControlCommandsSignalReconciliation()
{
    var now = new DateTimeOffset(2026, 5, 10, 13, 0, 0, TimeSpan.Zero);
    await using var context = CreateContext();
    var signal = new RecordingReconciliationSignal();
    var handler = new ControlCommandHandler(context, new CallbackTemplateRenderer(), new FixedTimeProvider(now), signal);
    var definition = new WatchDefinition(
        WatchId: "watch-signal",
        PathToWatch: "/mnt/watch/signal",
        CallbackUrlTemplate: "https://callback.test/{eventType}",
        CallbackPayloadTemplate: "{}");

    await handler.HandleAsync("command-signal", "create_watcher", definition);

    AssertEqual(1, signal.RequestCount, "control command reconciliation signal count");
}

static async Task VerifySupervisorPollsForDesiredStateChanges()
{
    var now = new DateTimeOffset(2026, 5, 10, 14, 0, 0, TimeSpan.Zero);
    await using var context = CreateContext();
    var runtime = new RecordingWatcherRuntime();
    var supervisor = new Supervisor(context, runtime);
    using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var running = supervisor.RunAsync(TimeSpan.FromMilliseconds(20), cancellation.Token);

    context.Watches.Add(new Watch
    {
        WatchId = "watch-polled",
        PathToWatch = "/mnt/watch/polled",
        Status = "active",
        CallbackUrlTemplate = "https://callback.test/{eventType}",
        CallbackPayloadTemplate = "{}",
        Version = 1,
        CreatedAt = now,
        UpdatedAt = now
    });
    await context.SaveChangesAsync();

    await WaitUntilAsync(() => runtime.StartedWatchIds.Contains("watch-polled"), cancellation.Token);
    await cancellation.CancelAsync();
    await running;

    AssertSequenceEqual(["watch-polled"], runtime.StartedWatchIds.ToArray(), "polled active watch ids");
}

static async Task VerifyFileSystemWatcherRuntimeRecordsLocalEvents()
{
    var watchPath = Path.Combine(Path.GetTempPath(), "media-ingest-local-fs-watcher-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(watchPath);

    try
    {
        var now = new DateTimeOffset(2026, 5, 10, 15, 0, 0, TimeSpan.Zero);
        var preexistingFilePath = Path.Combine(watchPath, "preexisting.mov");
        File.WriteAllText(preexistingFilePath, "not-real-media");
        await using var context = CreateContext();
        context.Watches.Add(new Watch
        {
            WatchId = "watch-runtime",
            PathToWatch = watchPath,
            Status = "active",
            CallbackUrlTemplate = "https://callback.test/{eventType}",
            CallbackPayloadTemplate = """{"path":"{targetEventSourcePath}"}""",
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();

        var recorder = new EventRecorder(context, new CallbackTemplateRenderer(), new FixedTimeProvider(now));
        var runtime = new FileSystemWatcherRuntime(recorder);

        await runtime.StartAsync(new WatchDefinition(
            "watch-runtime",
            watchPath,
            "https://callback.test/{eventType}",
            "{}"));
        File.WriteAllText(Path.Combine(watchPath, "source.mov"), "not-real-media");

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => context.Events.Any(), cancellation.Token);
        File.Delete(preexistingFilePath);
        await WaitUntilAsync(
            () => context.Events.Any(watchEvent => watchEvent.EventType == "deleted" && watchEvent.TargetEventSourcePath == preexistingFilePath),
            cancellation.Token);
        File.Delete(Path.Combine(watchPath, "source.mov"));
        await WaitUntilAsync(() => context.Events.Any(watchEvent => watchEvent.EventType == "deleted"), cancellation.Token);
        await runtime.StopAsync("watch-runtime");

        AssertTrue(context.Events.Any(watchEvent => watchEvent.EventType == "created"), "runtime recorded created event");
        AssertTrue(
            context.Events.Any(watchEvent => watchEvent.EventType == "deleted" && watchEvent.IsFile),
            "runtime recorded deleted file event");
        AssertTrue(
            context.Events.Any(watchEvent =>
                watchEvent.EventType == "deleted" &&
                watchEvent.IsFile &&
                watchEvent.TargetEventSourcePath == preexistingFilePath),
            "runtime classified preexisting deleted file event");
        AssertEqual(
            context.Events.Count(),
            context.OutboxMessages.Count(),
            "runtime queued one callback outbox message per event");
    }
    finally
    {
        Directory.Delete(watchPath, recursive: true);
    }
}

static WatcherDbContext CreateContext()
{
    var options = new DbContextOptionsBuilder<WatcherDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
        .UseSnakeCaseNamingConvention()
        .Options;

    return new WatcherDbContext(options);
}

static void AssertEntityTable<TEntity>(WatcherDbContext context, string expectedTableName)
{
    var entity = context.Model.FindEntityType(typeof(TEntity))
        ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} was not mapped.");

    AssertEqual(expectedTableName, entity.GetTableName(), $"{typeof(TEntity).Name} table name");
    AssertEqual("local_file_system_watcher", entity.GetSchema(), $"{typeof(TEntity).Name} schema");
}

static void AssertThrows<TException>(Action action, string label)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {label} to throw {typeof(TException).Name}.");
}

static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
{
    while (!condition())
    {
        await Task.Delay(20, cancellationToken);
    }
}

static void AssertTrue(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{label}: expected true.");
    }
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException(
            $"{label}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string label)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"{label}: expected '{string.Join(", ", expected)}', got '{string.Join(", ", actual)}'.");
    }
}

internal sealed class RecordingWatcherRuntime : IWatcherRuntime
{
    public List<string> StartedWatchIds { get; } = [];

    public List<string> StoppedWatchIds { get; } = [];

    public List<WatchDefinition> StartedDefinitions { get; } = [];

    public Task StartAsync(WatchDefinition definition, CancellationToken cancellationToken = default)
    {
        StartedWatchIds.Add(definition.WatchId);
        StartedDefinitions.Add(definition);
        return Task.CompletedTask;
    }

    public Task StopAsync(string watchId, CancellationToken cancellationToken = default)
    {
        StoppedWatchIds.Add(watchId);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingReconciliationSignal : IReconciliationSignal
{
    public int RequestCount { get; private set; }

    public void RequestReconciliation()
    {
        RequestCount++;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
