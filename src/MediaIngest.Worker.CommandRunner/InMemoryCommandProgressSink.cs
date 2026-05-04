using MediaIngest.Contracts.Commands;
using MediaIngest.Observability;

namespace MediaIngest.Worker.CommandRunner;

public sealed class InMemoryCommandProgressSink : ICommandProgressSink
{
    private readonly object syncRoot = new();
    private readonly List<CommandProgressRecord> records = [];

    public IReadOnlyList<CommandProgressRecord> Records
    {
        get
        {
            lock (syncRoot)
            {
                return records.ToArray();
            }
        }
    }

    public void Record(
        MediaCommandEnvelope envelope,
        ObservabilityCorrelationContext correlation,
        CommandProgressStatus status,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(correlation);

        var record = new CommandProgressRecord(
            CommandId: envelope.CommandId,
            CommandName: envelope.CommandName,
            ExecutionClass: envelope.ExecutionClass,
            Status: status,
            CorrelationFields: correlation.ToFields(),
            OccurredAt: DateTimeOffset.UtcNow,
            Message: message);

        lock (syncRoot)
        {
            records.Add(record);
        }
    }
}
