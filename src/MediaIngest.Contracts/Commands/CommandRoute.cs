namespace MediaIngest.Contracts.Commands;

public sealed record CommandRoute(
    string TopicName,
    ExecutionClass ExecutionClass)
{
    public const string ExecutionClassPropertyName = "executionClass";

    public IReadOnlyDictionary<string, string> ApplicationProperties =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ExecutionClassPropertyName] = ExecutionClass.ToPropertyValue()
        };
}
