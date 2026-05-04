using MediaIngest.Contracts.Commands;

namespace MediaIngest.Worker.CommandRunner;

public interface ICommandExecutor
{
    Task<CommandExecutionResult> ExecuteAsync(MediaCommandEnvelope envelope, CancellationToken cancellationToken = default);
}
