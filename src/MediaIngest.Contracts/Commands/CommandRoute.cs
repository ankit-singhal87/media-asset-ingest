namespace MediaIngest.Contracts.Commands;

public sealed record CommandRoute(
    string TopicName,
    ExecutionClass ExecutionClass);
