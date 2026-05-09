using MediaIngest.Contracts.Commands;
using MediaIngest.Persistence;
using MediaIngest.Worker.Outbox;

var connectionString = ReadSetting("Persistence__ConnectionString")
    ?? ReadSetting("ConnectionStrings__MediaIngest")
    ?? throw new InvalidOperationException(
        "Persistence__ConnectionString or ConnectionStrings__MediaIngest is required.");
var createSchemaOnStartup = bool.TryParse(
    ReadSetting("Persistence__CreateSchemaOnStartup"),
    out var createSchema) && createSchema;
var interval = TimeSpan.FromMilliseconds(ReadPositiveInt("Worker__IntervalMilliseconds", 1000));

var store = PostgresIngestPersistenceStore.Create(connectionString);
if (createSchemaOnStartup)
{
    await store.CreateSchemaAsync();
}

var dispatcher = new OutboxDispatcher(
    store,
    new LocalReviewOutboxMessagePublisher(new ServiceBusCommandBusAdapter()));

Console.WriteLine($"Media ingest outbox worker started. interval={interval}");

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => cancellation.Cancel();

try
{
    while (!cancellation.IsCancellationRequested)
    {
        var dispatched = await dispatcher.DispatchPendingAsync(cancellation.Token);
        if (dispatched > 0)
        {
            Console.WriteLine($"Dispatched {dispatched} outbox message(s).");
        }

        await Task.Delay(interval, cancellation.Token);
    }
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
}

static string? ReadSetting(string name)
{
    var value = Environment.GetEnvironmentVariable(name);

    return string.IsNullOrWhiteSpace(value) ? null : value;
}

static int ReadPositiveInt(string name, int defaultValue)
{
    var value = ReadSetting(name);
    if (value is null)
    {
        return defaultValue;
    }

    if (int.TryParse(value, out var parsed) && parsed > 0)
    {
        return parsed;
    }

    throw new InvalidOperationException($"{name} must be a positive integer.");
}

internal sealed class LocalReviewOutboxMessagePublisher(
    ServiceBusCommandBusAdapter serviceBusAdapter) : IOutboxMessagePublisher
{
    public Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.Message.MessageType, nameof(MediaCommandEnvelope), StringComparison.Ordinal))
        {
            var message = serviceBusAdapter.CreateMessage(request);
            Console.WriteLine(
                $"Validated command outbox message {request.Message.MessageId} for " +
                $"{message.TopicName}/{message.RoutedSubscriptionName}.");
            return Task.CompletedTask;
        }

        Console.WriteLine(
            $"Observed local outbox message {request.Message.MessageId} " +
            $"of type {request.Message.MessageType}.");
        return Task.CompletedTask;
    }
}
