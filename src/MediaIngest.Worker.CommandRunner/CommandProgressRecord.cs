using MediaIngest.Contracts.Commands;

namespace MediaIngest.Worker.CommandRunner;

public sealed record CommandProgressRecord(
    string CommandId,
    string CommandName,
    ExecutionClass ExecutionClass,
    CommandProgressStatus Status,
    IReadOnlyDictionary<string, string> CorrelationFields,
    DateTimeOffset OccurredAt,
    string? Message);
