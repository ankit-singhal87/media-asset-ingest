using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;

var store = new InMemoryIngestPersistenceStore();
var createdAt = new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero);
var message = new OutboxMessage(
    MessageId: "message-001",
    Destination: "media.command.create_proxy",
    MessageType: "MediaCommandEnvelope",
    PayloadJson: """{"packageId":"package-001"}""",
    CorrelationId: "correlation-001",
    CreatedAt: createdAt);

await store.SaveAsync(new PersistenceBatch([], [message]));

var publisher = new RecordingOutboxPublisher();
var dispatcher = new OutboxDispatcher(store, publisher);

var firstRun = await dispatcher.DispatchPendingAsync();
var secondRun = await dispatcher.DispatchPendingAsync();

AssertEqual(1, firstRun, "first dispatch count");
AssertEqual(0, secondRun, "second dispatch count");
AssertEqual(1, publisher.Published.Count, "published message count");
AssertEqual("media.command.create_proxy", publisher.Published[0].Destination, "published destination");
AssertEqual("MediaCommandEnvelope", publisher.Published[0].MessageType, "published message type");
AssertTrue(store.OutboxMessages[0].DispatchedAt is not null, "dispatched timestamp is recorded");

Console.WriteLine("MediaIngest outbox dispatcher smoke tests passed.");

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
    public List<OutboxMessage> Published { get; } = [];

    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}
