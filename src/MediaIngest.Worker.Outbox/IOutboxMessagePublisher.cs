using MediaIngest.Persistence;

namespace MediaIngest.Worker.Outbox;

public interface IOutboxMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
