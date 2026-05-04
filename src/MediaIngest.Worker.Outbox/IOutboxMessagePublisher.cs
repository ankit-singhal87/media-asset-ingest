using MediaIngest.Persistence;

namespace MediaIngest.Worker.Outbox;

public interface IOutboxMessagePublisher
{
    Task PublishAsync(OutboxPublishRequest request, CancellationToken cancellationToken = default);
}
