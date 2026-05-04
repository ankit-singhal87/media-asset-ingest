using System.Net;
using System.Text.Json;
using MediaIngest.Contracts.Commands;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;

await DispatchPendingMessagesPublishesAndMarksThemWithTheDispatchTime();
await DispatchPendingCommandMessagesPublishesExecutionClassMetadata();
await NonCommandMessagesDoNotPublishCommandApplicationProperties();
await OutboxPublishersCannotDropApplicationProperties();
await DispatchPendingMessagesDoesNothingWhenNoMessagesArePending();
await DispatchPendingMessagesLeavesTheMessagePendingWhenPublishFails();
await DispatchPendingMessagesMarksOnlySuccessfullyPublishedMessages();
await OverlappingDispatcherRunsDoNotPublishTheSameMessageTwice();
await ClaimedMessagesCanBeRetriedAfterTheClaimExpires();
await DaprPublisherPublishesPayloadToTheDestinationTopic();
await DaprPublisherRequestsRawPayloadToPreserveBrokerMessageBody();
await DaprPublisherMapsApplicationPropertiesToDaprMetadata();
await DaprPublisherSurfacesNonSuccessResponses();

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
        payloadJson: JsonSerializer.Serialize(new MediaCommandEnvelope(
            CommandId: "command-heavy",
            CommandName: CommandNames.ArchiveAsset,
            TopicName: CommandNames.ArchiveAsset,
            ExecutionClass: ExecutionClass.Heavy,
            CommandLine: "archive package-001",
            WorkingDirectory: "/mnt/work/package-001",
            InputPaths: ["/mnt/ingest/package-001/source.mov"],
            OutputPaths: ["/mnt/archive/package-001/source.mov"],
            CorrelationId: "correlation-001")));

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
    AssertEqual(
        "heavy",
        publisher.Published[0].ApplicationProperties[CommandRoute.ExecutionClassPropertyName],
        "command execution class property");
}

static async Task NonCommandMessagesDoNotPublishCommandApplicationProperties()
{
    var store = new InMemoryIngestPersistenceStore();
    var message = CreateMessage(
        messageId: "message-event-with-execution-class",
        destination: "media.event.package_scanned",
        messageType: "PackageScannedEvent",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 8, 0, TimeSpan.Zero),
        executionClass: "heavy");

    await store.SaveAsync(new PersistenceBatch([], [message]));

    var publisher = new RecordingOutboxPublisher();
    var dispatcher = new OutboxDispatcher(
        store,
        publisher,
        new FixedTimeProvider(new DateTimeOffset(2026, 5, 3, 12, 9, 0, TimeSpan.Zero)));

    var dispatchedCount = await dispatcher.DispatchPendingAsync();

    AssertEqual(1, dispatchedCount, "non-command dispatch count");
    AssertEqual(1, publisher.Published.Count, "non-command publish request count");
    AssertEqual(0, publisher.Published[0].ApplicationProperties.Count, "non-command application property count");
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

static async Task DispatchPendingMessagesMarksOnlySuccessfullyPublishedMessages()
{
    var store = new InMemoryIngestPersistenceStore();
    var firstMessage = CreateMessage(
        messageId: "message-success-before-failure",
        destination: "media.command.create_proxy",
        messageType: "MediaCommandEnvelope",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 16, 0, TimeSpan.Zero));
    var secondMessage = CreateMessage(
        messageId: "message-failure-after-success",
        destination: "media.command.create_thumbnail",
        messageType: "MediaCommandEnvelope",
        createdAt: new DateTimeOffset(2026, 5, 3, 12, 17, 0, TimeSpan.Zero));

    await store.SaveAsync(new PersistenceBatch([], [firstMessage, secondMessage]));

    var publisher = new FailsOnSecondPublishOutboxPublisher("broker unavailable");
    var dispatchTime = new DateTimeOffset(2026, 5, 3, 12, 18, 0, TimeSpan.Zero);
    var dispatcher = new OutboxDispatcher(store, publisher, new FixedTimeProvider(dispatchTime));
    var failed = false;

    try
    {
        await dispatcher.DispatchPendingAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message == "broker unavailable")
    {
        failed = true;
    }

    AssertTrue(failed, "second publish failure is surfaced");
    AssertEqual(2, publisher.PublishAttempts, "publish attempts before failure");
    AssertEqual(dispatchTime, store.OutboxMessages[0].DispatchedAt, "successful message is marked dispatched");
    AssertEqual(null, store.OutboxMessages[1].DispatchedAt, "failed message remains pending");
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

static async Task DaprPublisherPublishesPayloadToTheDestinationTopic()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.NoContent, "");
    using var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://127.0.0.1:3500")
    };
    var publisher = new DaprOutboxMessagePublisher(httpClient, "commandbus");
    var request = new OutboxPublishRequest(
        CreateMessage(
            messageId: "message-dapr-001",
            destination: "media.command.create_proxy",
            messageType: "MediaCommandEnvelope",
            createdAt: new DateTimeOffset(2026, 5, 3, 12, 40, 0, TimeSpan.Zero),
            payloadJson: """{"commandId":"command-001","executionClass":"light"}"""),
        new Dictionary<string, string>(StringComparer.Ordinal));

    await publisher.PublishAsync(request);

    AssertEqual(HttpMethod.Post, handler.Requests[0].Method, "dapr publish method");
    AssertEqual(
        "http://127.0.0.1:3500/v1.0/publish/commandbus/media.command.create_proxy?metadata.rawPayload=true",
        handler.Requests[0].RequestUri?.ToString(),
        "dapr publish topic URL");
    AssertEqual(
        """{"commandId":"command-001","executionClass":"light"}""",
        handler.RequestBodies[0],
        "dapr publish payload body");
    AssertEqual("application/json; charset=utf-8", handler.Requests[0].Content?.Headers.ContentType?.ToString(), "dapr content type");
}

static async Task DaprPublisherRequestsRawPayloadToPreserveBrokerMessageBody()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.NoContent, "");
    using var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://127.0.0.1:3500")
    };
    var publisher = new DaprOutboxMessagePublisher(httpClient, "commandbus");
    var request = new OutboxPublishRequest(
        CreateMessage(
            messageId: "message-dapr-raw-payload",
            destination: "media.command.create_proxy",
            messageType: "MediaCommandEnvelope",
            createdAt: new DateTimeOffset(2026, 5, 3, 12, 42, 0, TimeSpan.Zero),
            payloadJson: """{"commandId":"command-raw","executionClass":"light"}"""),
        new Dictionary<string, string>(StringComparer.Ordinal));

    await publisher.PublishAsync(request);

    AssertEqual(
        "http://127.0.0.1:3500/v1.0/publish/commandbus/media.command.create_proxy?metadata.rawPayload=true",
        handler.Requests[0].RequestUri?.ToString(),
        "dapr raw payload metadata URL");
    AssertEqual(
        """{"commandId":"command-raw","executionClass":"light"}""",
        handler.RequestBodies[0],
        "dapr raw payload body");
}

static async Task DaprPublisherMapsApplicationPropertiesToDaprMetadata()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.NoContent, "");
    using var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://127.0.0.1:3500")
    };
    var publisher = new DaprOutboxMessagePublisher(httpClient, "commandbus");
    var request = new OutboxPublishRequest(
        CreateMessage(
            messageId: "message-dapr-002",
            destination: "media.command.archive_asset",
            messageType: "MediaCommandEnvelope",
            createdAt: new DateTimeOffset(2026, 5, 3, 12, 45, 0, TimeSpan.Zero),
            payloadJson: """{"commandId":"command-heavy","executionClass":"heavy"}"""),
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CommandRoute.ExecutionClassPropertyName] = "heavy"
        });

    await publisher.PublishAsync(request);

    AssertEqual(
        "http://127.0.0.1:3500/v1.0/publish/commandbus/media.command.archive_asset?metadata.executionClass=heavy&metadata.rawPayload=true",
        handler.Requests[0].RequestUri?.ToString(),
        "dapr publish metadata URL");
}

static async Task DaprPublisherSurfacesNonSuccessResponses()
{
    var handler = new RecordingHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "sidecar unavailable");
    using var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://127.0.0.1:3500")
    };
    var publisher = new DaprOutboxMessagePublisher(httpClient, "commandbus");
    var request = new OutboxPublishRequest(
        CreateMessage(
            messageId: "message-dapr-003",
            destination: "media.command.verify_checksum",
            messageType: "MediaCommandEnvelope",
            createdAt: new DateTimeOffset(2026, 5, 3, 12, 50, 0, TimeSpan.Zero)),
        new Dictionary<string, string>(StringComparer.Ordinal));
    var failed = false;

    try
    {
        await publisher.PublishAsync(request);
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("Dapr publish failed", StringComparison.Ordinal)
        && ex.Message.Contains("503", StringComparison.Ordinal)
        && ex.Message.Contains("message-dapr-003", StringComparison.Ordinal)
        && ex.Message.Contains("sidecar unavailable", StringComparison.Ordinal))
    {
        failed = true;
    }

    AssertTrue(failed, "dapr non-success response is surfaced");
}

static OutboxMessage CreateMessage(
    string messageId,
    string destination,
    string messageType,
    DateTimeOffset createdAt,
    string executionClass = "light",
    string? payloadJson = null)
{
    return new OutboxMessage(
        MessageId: messageId,
        Destination: destination,
        MessageType: messageType,
        PayloadJson: payloadJson ?? $$"""{"packageId":"package-001","executionClass":"{{executionClass}}"}""",
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

internal sealed class FailsOnSecondPublishOutboxPublisher(string failureMessage) : IOutboxMessagePublisher
{
    public int PublishAttempts { get; private set; }

    public Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        _ = request;
        cancellationToken.ThrowIfCancellationRequested();
        PublishAttempts++;

        if (PublishAttempts == 2)
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

internal sealed class RecordingHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];
    public List<string> RequestBodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null
            ? ""
            : await request.Content.ReadAsStringAsync(cancellationToken));

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody)
        };
    }
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
