namespace MediaIngest.Worker.Outbox;

public sealed class CommandBusOutboxMessagePublisher(
    ServiceBusCommandBusAdapter serviceBusAdapter,
    IOutboxMessagePublisher localPublisher)
    : IOutboxMessagePublisher
{
    public async Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default)
    {
        _ = serviceBusAdapter.CreateMessage(request);

        await localPublisher.PublishAsync(request, cancellationToken);
    }
}
