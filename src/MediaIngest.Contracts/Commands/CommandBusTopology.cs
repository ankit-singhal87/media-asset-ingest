namespace MediaIngest.Contracts.Commands;

public sealed record CommandBusTopicTopology(
    string TopicName,
    IReadOnlyList<CommandBusSubscriptionTopology> Subscriptions);

public sealed record CommandBusSubscriptionTopology(
    string SubscriptionName,
    string FilterPropertyName,
    string FilterPropertyValue)
{
    public string SqlFilterExpression => $"{FilterPropertyName} = '{FilterPropertyValue}'";
}

public static class CommandBusTopology
{
    public const string LightSubscriptionName = ExecutionClassProperties.Light;
    public const string MediumSubscriptionName = ExecutionClassProperties.Medium;
    public const string HeavySubscriptionName = ExecutionClassProperties.Heavy;

    public static IReadOnlyList<string> CommandTopics { get; } =
    [
        CommandNames.CreateProxy,
        CommandNames.CreateChecksum,
        CommandNames.VerifyChecksum,
        CommandNames.RunSecurityScan,
        CommandNames.ArchiveAsset
    ];

    public static IReadOnlyList<CommandBusSubscriptionTopology> Subscriptions { get; } =
    [
        CreateSubscription(LightSubscriptionName),
        CreateSubscription(MediumSubscriptionName),
        CreateSubscription(HeavySubscriptionName)
    ];

    public static IReadOnlyList<CommandBusTopicTopology> Topics { get; } =
        CommandTopics
            .Select(topicName => new CommandBusTopicTopology(
                TopicName: topicName,
                Subscriptions: Subscriptions))
            .ToArray();

    private static CommandBusSubscriptionTopology CreateSubscription(string executionClass)
    {
        return new CommandBusSubscriptionTopology(
            SubscriptionName: executionClass,
            FilterPropertyName: CommandRoute.ExecutionClassPropertyName,
            FilterPropertyValue: executionClass);
    }
}
