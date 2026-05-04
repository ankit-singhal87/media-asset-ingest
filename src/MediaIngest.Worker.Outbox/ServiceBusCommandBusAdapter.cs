using MediaIngest.Contracts.Commands;

namespace MediaIngest.Worker.Outbox;

public sealed record ServiceBusCommandBusMessage(
    string TopicName,
    string RoutedSubscriptionName,
    string BodyJson,
    IReadOnlyDictionary<string, string> ApplicationProperties);

public sealed class ServiceBusCommandBusAdapter
{
    public ServiceBusCommandBusMessage CreateMessage(OutboxPublishRequest request)
    {
        if (!string.Equals(request.Message.MessageType, nameof(MediaCommandEnvelope), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Outbox message '{request.Message.MessageId}' must be a {nameof(MediaCommandEnvelope)} command.");
        }

        if (!CommandBusTopology.CommandTopics.Contains(request.Message.Destination, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Command outbox message '{request.Message.MessageId}' targets unknown command topic " +
                $"'{request.Message.Destination}'.");
        }

        if (!request.ApplicationProperties.TryGetValue(
                CommandRoute.ExecutionClassPropertyName,
                out var executionClass))
        {
            throw new InvalidOperationException(
                $"Command outbox message '{request.Message.MessageId}' must include an " +
                $"{CommandRoute.ExecutionClassPropertyName} application property.");
        }

        var normalizedExecutionClass = ExecutionClassProperties
            .FromPropertyValue(executionClass)
            .ToPropertyValue();
        var subscription = CommandBusTopology.Subscriptions.SingleOrDefault(candidate =>
            string.Equals(candidate.FilterPropertyName, CommandRoute.ExecutionClassPropertyName, StringComparison.Ordinal)
            && string.Equals(candidate.FilterPropertyValue, normalizedExecutionClass, StringComparison.Ordinal));

        if (subscription is null)
        {
            throw new InvalidOperationException(
                $"Command outbox message '{request.Message.MessageId}' execution class " +
                $"'{normalizedExecutionClass}' does not map to a command-bus subscription.");
        }

        var applicationProperties = new Dictionary<string, string>(
            request.ApplicationProperties,
            StringComparer.Ordinal)
        {
            [CommandRoute.ExecutionClassPropertyName] = normalizedExecutionClass
        };

        return new ServiceBusCommandBusMessage(
            TopicName: request.Message.Destination,
            RoutedSubscriptionName: subscription.SubscriptionName,
            BodyJson: request.Message.PayloadJson,
            ApplicationProperties: applicationProperties);
    }
}
