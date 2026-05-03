namespace MediaIngest.Contracts.Commands;

public sealed record MediaCommandEnvelope(
    string CommandId,
    string CommandName,
    string TopicName,
    ExecutionClass ExecutionClass,
    string CommandLine,
    string WorkingDirectory,
    IReadOnlyList<string> InputPaths,
    IReadOnlyList<string> OutputPaths,
    string CorrelationId);
