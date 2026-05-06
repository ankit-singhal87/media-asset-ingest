namespace MediaIngest.Worker.CommandRunner;

public sealed record CommandBusReceivedMessage(
    string MessageId,
    string TopicName,
    string SubscriptionName,
    string BodyJson,
    IReadOnlyDictionary<string, string> ApplicationProperties);
