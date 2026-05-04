using MediaIngest.Contracts.Commands;
using MediaIngest.Observability;

namespace MediaIngest.Worker.CommandRunner;

public sealed class GenericCommandRunner
{
    private readonly object syncRoot = new();
    private readonly HashSet<string> handledCommandIds = new(StringComparer.Ordinal);
    private readonly ICommandExecutor executor;
    private readonly ICommandProgressSink progressSink;

    public GenericCommandRunner(
        ExecutionClass configuredExecutionClass,
        ICommandExecutor executor,
        ICommandProgressSink progressSink)
    {
        ConfiguredExecutionClass = configuredExecutionClass;
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.progressSink = progressSink ?? throw new ArgumentNullException(nameof(progressSink));
    }

    public ExecutionClass ConfiguredExecutionClass { get; }

    public async Task<CommandHandlingResult> HandleAsync(
        MediaCommandEnvelope envelope,
        ObservabilityCorrelationContext correlation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(correlation);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.CommandId);

        if (envelope.ExecutionClass != ConfiguredExecutionClass)
        {
            var message =
                $"Runner configured for {ConfiguredExecutionClass.ToPropertyValue()} cannot handle {envelope.ExecutionClass.ToPropertyValue()} command.";
            progressSink.Record(envelope, correlation, CommandProgressStatus.Rejected, message);
            return CommandHandlingResult.RejectedExecutionClass(message);
        }

        lock (syncRoot)
        {
            if (!handledCommandIds.Add(envelope.CommandId))
            {
                var message = $"Command {envelope.CommandId} was already handled.";
                progressSink.Record(envelope, correlation, CommandProgressStatus.DuplicateSkipped, message);
                return CommandHandlingResult.Duplicate(message);
            }
        }

        progressSink.Record(envelope, correlation, CommandProgressStatus.Accepted, "Command accepted.");

        try
        {
            var executionResult = await executor.ExecuteAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (executionResult.IsSuccess)
            {
                progressSink.Record(envelope, correlation, CommandProgressStatus.Succeeded, executionResult.Message);
                return CommandHandlingResult.Succeeded(executionResult.Message);
            }

            var failureMessage = executionResult.Message ?? "Command execution failed.";
            progressSink.Record(envelope, correlation, CommandProgressStatus.Failed, failureMessage);
            return CommandHandlingResult.Failed(failureMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            progressSink.Record(envelope, correlation, CommandProgressStatus.Failed, ex.Message);
            return CommandHandlingResult.Failed(ex.Message);
        }
    }
}
