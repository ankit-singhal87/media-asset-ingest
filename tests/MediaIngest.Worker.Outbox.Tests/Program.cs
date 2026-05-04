using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;

await DispatchPendingMessagesPublishesAndMarksThemWithTheDispatchTime();
await DispatchPendingCommandMessagesPublishesExecutionClassMetadata();
await OutboxPublishersCannotDropApplicationProperties();
await DispatchPendingMessagesDoesNothingWhenNoMessagesArePending();
await DispatchPendingMessagesLeavesTheMessagePendingWhenPublishFails();
await OverlappingDispatcherRunsDoNotPublishTheSameMessageTwice();
await ClaimedMessagesCanBeRetriedAfterTheClaimExpires();

Console.WriteLine("MediaIngest outbox dispatcher smoke tests passed.");

static async Task DispatchPendingMessagesPublishesAndMarksThemWithTheDispatchTime()
{
    var store = new InMemoryIngestPersistenceStore();
    var createdAt = new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero);
    var dispatchedAt = new DateTimeOffset(2026, 5, 3, 12, 5, 0, TimeSpan.Zero);
    var message = CreateMessage(
        messageId: "message-001",
        destination: "media.command.create_proxy",
        messageType: "MediaCommandEnvelope",
        createdAt: createdAt);

    await store.SaveAsync(new PersistenceBatch([], [message]));

    var publisher = new RecordingOutboxPublisher();
    var dispatcher = new OutboxDispatcher(store, publisher, new FixedTimeProvider(dispatchedAt));

    var firstRun = await dispatcher.DispatchPendingAsync();
    var secondRun = await dispatcher.DispatchPendingAsync();

    AssertEqual(1, firstRun, "first dispatch count");
    AssertEqual(0, secondRun, "second dispatch count");
    AssertEqual(1, publisher.Published.Count, "published message count");
    AssertEqual("media.command.create_proxy", publisher.Published[0].Message.Destination, "published destination");
    AssertEqual("MediaCommandEnvelope", publisher.Published[0].Message.MessageType, "published message type");
    AssertEqual(dispatchedAt, store.OutboxMessages[0].DispatchedAt, "dispatched timestamp");
}

static async Task DispatchPendingCommandMessagesPublishesExecutionClassMetadata()
{
    var store = new InMemoryIngestPersistenceStore();
    var message = CreateMessage(
        messageId: "message-command-heavy",
        destination: "media.command.archive_asset",
        messageType: "MediaCommandEnvelope",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 6, 0, TimeSpan.Zero),
        executionClass: "heavy");

    await store.SaveAsync(new PersistenceBatch([], [message]));

    var publisher = new RecordingOutboxPublisher();
    var dispatcher = new OutboxDispatcher(
        store,
        publisher,
        new FixedTimeProvider(new DateTimeOffset(2026, 5, 3, 12, 7, 0, TimeSpan.Zero)));

    var dispatchedCount = await dispatcher.DispatchPendingAsync();

    AssertEqual(1, dispatchedCount, "command dispatch count");
    AssertEqual(1, publisher.Published.Count, "command publish request count");
    AssertEqual("media.command.archive_asset", publisher.Published[0].Message.Destination, "command publish destination");
    AssertEqual("heavy", publisher.Published[0].ApplicationProperties["executionClass"], "command execution class property");
}

static Task OutboxPublishersCannotDropApplicationProperties()
{
    var publisherMethods = typeof(IOutboxMessagePublisher)
        .GetMethods()
        .Where(method => method.Name == nameof(IOutboxMessagePublisher.PublishAsync))
        .ToArray();

    AssertEqual(1, publisherMethods.Length, "publisher publish overload count");
    AssertEqual(typeof(OutboxPublishRequest), publisherMethods[0].GetParameters()[0].ParameterType, "publisher request type");

    return Task.CompletedTask;
}

static async Task DispatchPendingMessagesDoesNothingWhenNoMessagesArePending()
{
    var store = new InMemoryIngestPersistenceStore();
    var publisher = new RecordingOutboxPublisher();
    var dispatcher = new OutboxDispatcher(store, publisher, new FixedTimeProvider(DateTimeOffset.UnixEpoch));

    var dispatchedCount = await dispatcher.DispatchPendingAsync();

    AssertEqual(0, dispatchedCount, "empty dispatch count");
    AssertEqual(0, publisher.Published.Count, "empty published message count");
}

static async Task DispatchPendingMessagesLeavesTheMessagePendingWhenPublishFails()
{
    var store = new InMemoryIngestPersistenceStore();
    var message = CreateMessage(
        messageId: "message-002",
        destination: "media.command.create_thumbnail",
        messageType: "MediaCommandEnvelope",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 10, 0, TimeSpan.Zero));

    await store.SaveAsync(new PersistenceBatch([], [message]));

    var dispatcher = new OutboxDispatcher(
        store,
        new FailingOutboxPublisher("broker unavailable"),
        new FixedTimeProvider(new DateTimeOffset(2026, 5, 3, 12, 15, 0, TimeSpan.Zero)));

    var failed = false;

    try
    {
        await dispatcher.DispatchPendingAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message == "broker unavailable")
    {
        failed = true;
    }

    AssertTrue(failed, "publish failure is surfaced");
    AssertEqual(null, store.OutboxMessages[0].DispatchedAt, "failed publish leaves message pending");
}

static async Task OverlappingDispatcherRunsDoNotPublishTheSameMessageTwice()
{
    var store = new InMemoryIngestPersistenceStore();
    var message = CreateMessage(
        messageId: "message-003",
        destination: "media.command.inspect",
        messageType: "MediaCommandEnvelope",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 20, 0, TimeSpan.Zero));

    await store.SaveAsync(new PersistenceBatch([], [message]));

    var publisher = new BlockingOutboxPublisher();
    var dispatchClock = new FixedTimeProvider(new DateTimeOffset(2026, 5, 3, 12, 25, 0, TimeSpan.Zero));
    var firstDispatcher = new OutboxDispatcher(store, publisher, dispatchClock);
    var secondDispatcher = new OutboxDispatcher(store, publisher, dispatchClock);

    var firstRun = firstDispatcher.DispatchPendingAsync();
    await publisher.WaitForFirstPublishAttemptAsync();

    var secondRun = secondDispatcher.DispatchPendingAsync();
    await Task.Delay(TimeSpan.FromMilliseconds(50));

    publisher.AllowPublishes();

    var dispatchCounts = await Task.WhenAll(firstRun, secondRun);

    AssertEqual(1, dispatchCounts.Sum(), "overlapping dispatch count");
    AssertEqual(1, publisher.Published.Count, "overlapping published message count");
    AssertEqual("message-003", publisher.Published[0].Message.MessageId, "overlapping published message id");
}

static async Task ClaimedMessagesCanBeRetriedAfterTheClaimExpires()
{
    var store = new InMemoryIngestPersistenceStore();
    var message = CreateMessage(
        messageId: "message-004",
        destination: "media.command.retry",
        messageType: "MediaCommandEnvelope",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 30, 0, TimeSpan.Zero));

    await store.SaveAsync(new PersistenceBatch([], [message]));

    var timeProvider = new MutableTimeProvider(new DateTimeOffset(2026, 5, 3, 12, 35, 0, TimeSpan.Zero));
    var publisher = new FailsOnceOutboxPublisher("broker unavailable");
    var dispatcher = new OutboxDispatcher(store, publisher, timeProvider, TimeSpan.FromSeconds(5));

    var failed = false;

    try
    {
        await dispatcher.DispatchPendingAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message == "broker unavailable")
    {
        failed = true;
    }

    AssertTrue(failed, "first publish failure is surfaced");

    var claimedRunCount = await dispatcher.DispatchPendingAsync();

    timeProvider.UtcNow = timeProvider.UtcNow.AddSeconds(5);

    var retryRunCount = await dispatcher.DispatchPendingAsync();

    AssertEqual(0, claimedRunCount, "claimed message dispatch count before expiry");
    AssertEqual(1, retryRunCount, "expired claim retry dispatch count");
    AssertEqual(2, publisher.PublishAttempts, "expired claim retry publish attempts");
    AssertEqual(timeProvider.UtcNow, store.OutboxMessages[0].DispatchedAt, "expired claim retry dispatched timestamp");
}

static OutboxMessage CreateMessage(
    string messageId,
    string destination,
    string messageType,
    DateTimeOffset createdAt,
    string executionClass = "light")
{
    return new OutboxMessage(
        MessageId: messageId,
        Destination: destination,
        MessageType: messageType,
        PayloadJson: $$"""{"packageId":"package-001","executionClass":"{{executionClass}}"}""",
        CorrelationId: "correlation-001",
        CreatedAt: createdAt);
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true.");
    }
}

internal sealed class RecordingOutboxPublisher : IOutboxMessagePublisher
{
    public List<OutboxPublishRequest> Published { get; } = [];

    public Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        Published.Add(request);
        return Task.CompletedTask;
    }
}

internal sealed class FailingOutboxPublisher(string failureMessage) : IOutboxMessagePublisher
{
    public Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        _ = request;
        throw new InvalidOperationException(failureMessage);
    }
}

internal sealed class FailsOnceOutboxPublisher(string failureMessage) : IOutboxMessagePublisher
{
    public int PublishAttempts { get; private set; }

    public Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        _ = request;
        cancellationToken.ThrowIfCancellationRequested();
        PublishAttempts++;

        if (PublishAttempts == 1)
        {
            throw new InvalidOperationException(failureMessage);
        }

        return Task.CompletedTask;
    }
}

internal sealed class BlockingOutboxPublisher : IOutboxMessagePublisher
{
    private readonly TaskCompletionSource firstPublishAttempted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource allowPublishes = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public List<OutboxPublishRequest> Published { get; } = [];

    public async Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        Published.Add(request);
        firstPublishAttempted.TrySetResult();
        await allowPublishes.Task.WaitAsync(cancellationToken);
    }

    public Task WaitForFirstPublishAttemptAsync() => firstPublishAttempted.Task;

    public void AllowPublishes() => allowPublishes.TrySetResult();
}

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return utcNow;
    }
}

internal sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow()
    {
        return UtcNow;
    }
}
