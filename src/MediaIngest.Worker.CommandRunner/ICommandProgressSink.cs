using MediaIngest.Contracts.Commands;
using MediaIngest.Observability;

namespace MediaIngest.Worker.CommandRunner;

public interface ICommandProgressSink
{
    void Record(
        MediaCommandEnvelope envelope,
        ObservabilityCorrelationContext correlation,
        CommandProgressStatus status,
        string? message = null);
}
