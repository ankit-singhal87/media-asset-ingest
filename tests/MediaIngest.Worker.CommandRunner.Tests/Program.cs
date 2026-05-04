using MediaIngest.Contracts.Commands;
using MediaIngest.Observability;
using MediaIngest.Worker.CommandRunner;

foreach (var executionClass in new[] { ExecutionClass.Light, ExecutionClass.Medium, ExecutionClass.Heavy })
{
    var executor = new RecordingCommandExecutor();
    var progress = new InMemoryCommandProgressSink();
    var runner = new GenericCommandRunner(executionClass, executor, progress);
    var envelope = CreateEnvelope(executionClass, $"command-{executionClass.ToPropertyValue()}");
    var correlation = CreateCorrelation(envelope);

    var result = await runner.HandleAsync(envelope, correlation);

    AssertEqual(CommandHandlingStatus.Succeeded, result.Status, $"{executionClass} runner accepts matching class");
    AssertEqual(1, executor.ExecutionCount, $"{executionClass} command executes once");
    AssertEqual(CommandProgressStatus.Accepted, progress.Records[0].Status, $"{executionClass} progress accepted");
    AssertEqual(CommandProgressStatus.Succeeded, progress.Records[1].Status, $"{executionClass} progress succeeded");
}

var mismatchedExecutor = new RecordingCommandExecutor();
var mismatchedProgress = new InMemoryCommandProgressSink();
var lightRunner = new GenericCommandRunner(ExecutionClass.Light, mismatchedExecutor, mismatchedProgress);
var heavyEnvelope = CreateEnvelope(ExecutionClass.Heavy, "command-heavy-on-light");

var mismatchedResult = await lightRunner.HandleAsync(heavyEnvelope, CreateCorrelation(heavyEnvelope));

AssertEqual(CommandHandlingStatus.RejectedExecutionClass, mismatchedResult.Status, "mismatched class is rejected");
AssertEqual(0, mismatchedExecutor.ExecutionCount, "mismatched command is not executed");
AssertEqual(CommandProgressStatus.Rejected, mismatchedProgress.Records.Single().Status, "mismatched rejection is recorded");
AssertProgressCorrelation(CreateCorrelation(heavyEnvelope), heavyEnvelope, mismatchedProgress.Records.Single(), "mismatched rejection");

var duplicateExecutor = new RecordingCommandExecutor();
var duplicateProgress = new InMemoryCommandProgressSink();
var duplicateRunner = new GenericCommandRunner(ExecutionClass.Medium, duplicateExecutor, duplicateProgress);
var duplicateEnvelope = CreateEnvelope(ExecutionClass.Medium, "command-duplicate");
var duplicateCorrelation = CreateCorrelation(duplicateEnvelope);

var firstResult = await duplicateRunner.HandleAsync(duplicateEnvelope, duplicateCorrelation);
var secondResult = await duplicateRunner.HandleAsync(duplicateEnvelope, duplicateCorrelation);

AssertEqual(CommandHandlingStatus.Succeeded, firstResult.Status, "first duplicate command succeeds");
AssertEqual(CommandHandlingStatus.Duplicate, secondResult.Status, "second duplicate command is idempotent");
AssertEqual(1, duplicateExecutor.ExecutionCount, "duplicate command executes only once");
AssertEqual(CommandProgressStatus.DuplicateSkipped, duplicateProgress.Records.Last().Status, "duplicate skip is recorded");
AssertSequence(
    [CommandProgressStatus.Accepted, CommandProgressStatus.Succeeded, CommandProgressStatus.DuplicateSkipped],
    duplicateProgress.Records.Select(static record => record.Status).ToArray(),
    "duplicate progress sequence");
AssertProgressCorrelation(duplicateCorrelation, duplicateEnvelope, duplicateProgress.Records.Last(), "duplicate skip");

var failedExecutor = new RecordingCommandExecutor { NextResult = CommandExecutionResult.Failed("exit-code-1") };
var failedProgress = new InMemoryCommandProgressSink();
var failedRunner = new GenericCommandRunner(ExecutionClass.Heavy, failedExecutor, failedProgress);
var failedEnvelope = CreateEnvelope(ExecutionClass.Heavy, "command-failed");

var failedResult = await failedRunner.HandleAsync(failedEnvelope, CreateCorrelation(failedEnvelope));

AssertEqual(CommandHandlingStatus.Failed, failedResult.Status, "failed command returns failed status");
AssertEqual("exit-code-1", failedResult.Message, "failed command preserves failure message");
AssertEqual(CommandProgressStatus.Failed, failedProgress.Records.Last().Status, "failed command is recorded");
AssertSequence(
    [CommandProgressStatus.Accepted, CommandProgressStatus.Failed],
    failedProgress.Records.Select(static record => record.Status).ToArray(),
    "failed progress sequence");

var retryExecutor = new QueueingCommandExecutor(
    CommandExecutionResult.Failed("transient-exit-code-1"),
    CommandExecutionResult.Succeeded("retry-ok"));
var retryProgress = new InMemoryCommandProgressSink();
var retryRunner = new GenericCommandRunner(ExecutionClass.Heavy, retryExecutor, retryProgress);
var retryEnvelope = CreateEnvelope(ExecutionClass.Heavy, "command-retry-after-failure");
var retryCorrelation = CreateCorrelation(retryEnvelope);

var retryFirstResult = await retryRunner.HandleAsync(retryEnvelope, retryCorrelation);
var retrySecondResult = await retryRunner.HandleAsync(retryEnvelope, retryCorrelation);

AssertEqual(CommandHandlingStatus.Failed, retryFirstResult.Status, "failed command reports failure before retry");
AssertEqual(CommandHandlingStatus.Succeeded, retrySecondResult.Status, "failed command can be retried");
AssertEqual(2, retryExecutor.ExecutionCount, "failed command is not treated as handled");
AssertSequence(
    [
        CommandProgressStatus.Accepted,
        CommandProgressStatus.Failed,
        CommandProgressStatus.Accepted,
        CommandProgressStatus.Succeeded
    ],
    retryProgress.Records.Select(static record => record.Status).ToArray(),
    "retry after failure progress sequence");

var throwingExecutor = new ThrowThenSucceedCommandExecutor();
var throwingProgress = new InMemoryCommandProgressSink();
var throwingRunner = new GenericCommandRunner(ExecutionClass.Light, throwingExecutor, throwingProgress);
var throwingEnvelope = CreateEnvelope(ExecutionClass.Light, "command-retry-after-exception");
var throwingCorrelation = CreateCorrelation(throwingEnvelope);

var throwingFirstResult = await throwingRunner.HandleAsync(throwingEnvelope, throwingCorrelation);
var throwingSecondResult = await throwingRunner.HandleAsync(throwingEnvelope, throwingCorrelation);

AssertEqual(CommandHandlingStatus.Failed, throwingFirstResult.Status, "executor exception reports failure before retry");
AssertEqual("executor unavailable", throwingFirstResult.Message, "executor exception message is preserved");
AssertEqual(CommandHandlingStatus.Succeeded, throwingSecondResult.Status, "executor exception does not poison retries");
AssertEqual(2, throwingExecutor.ExecutionCount, "executor exception does not mark command handled");

var correlationExecutor = new RecordingCommandExecutor();
var correlationProgress = new InMemoryCommandProgressSink();
var correlationRunner = new GenericCommandRunner(ExecutionClass.Light, correlationExecutor, correlationProgress);
var correlationEnvelope = CreateEnvelope(ExecutionClass.Light, "command-correlation");
var expectedCorrelation = CreateCorrelation(correlationEnvelope);

await correlationRunner.HandleAsync(correlationEnvelope, expectedCorrelation);

var progressFields = correlationProgress.Records.First().CorrelationFields;
foreach (var fieldName in CorrelationFieldNames.All)
{
    AssertTrue(progressFields.ContainsKey(fieldName), $"progress includes {fieldName}");
}

AssertEqual(expectedCorrelation.WorkflowInstanceId, progressFields[CorrelationFieldNames.WorkflowInstanceId], "workflow correlation field");
AssertEqual(expectedCorrelation.PackageId, progressFields[CorrelationFieldNames.PackageId], "package correlation field");
AssertEqual(expectedCorrelation.FileId, progressFields[CorrelationFieldNames.FileId], "file correlation field");
AssertEqual(expectedCorrelation.WorkItemId, progressFields[CorrelationFieldNames.WorkItemId], "work item correlation field");
AssertEqual(expectedCorrelation.NodeId, progressFields[CorrelationFieldNames.NodeId], "node correlation field");
AssertEqual(expectedCorrelation.AgentType, progressFields[CorrelationFieldNames.AgentType], "agent type correlation field");
AssertEqual(expectedCorrelation.QueueName, progressFields[CorrelationFieldNames.QueueName], "queue correlation field");
AssertEqual(correlationEnvelope.CorrelationId, progressFields[CorrelationFieldNames.CorrelationId], "command correlation field");
AssertEqual(correlationEnvelope.CommandId, progressFields[CorrelationFieldNames.CausationId], "command causation field");
AssertEqual(expectedCorrelation.TraceId, progressFields[CorrelationFieldNames.TraceId], "trace correlation field");
AssertEqual(expectedCorrelation.SpanId, progressFields[CorrelationFieldNames.SpanId], "span correlation field");

var processWorkDirectory = Directory.CreateTempSubdirectory("media-ingest-command-runner-tests-");
try
{
    var processExecutor = new LocalProcessCommandExecutor(TimeSpan.FromSeconds(5), maxCapturedOutputCharacters: 64);
    var processEnvelope = CreateEnvelope(ExecutionClass.Light, "command-local-process") with
    {
        CommandLine = "printf local-ok",
        WorkingDirectory = processWorkDirectory.FullName
    };

    var processResult = await processExecutor.ExecuteAsync(processEnvelope);

    AssertTrue(processResult.IsSuccess, "local process executor succeeds on zero exit code");
    AssertEqual("exit-code-0 stdout: local-ok", processResult.Message, "local process executor captures stdout");

    var boundedExecutor = new LocalProcessCommandExecutor(TimeSpan.FromSeconds(5), maxCapturedOutputCharacters: 4);
    var boundedEnvelope = CreateEnvelope(ExecutionClass.Light, "command-local-process-bounded") with
    {
        CommandLine = "printf 1234567890; exit 7",
        WorkingDirectory = processWorkDirectory.FullName
    };

    var boundedResult = await boundedExecutor.ExecuteAsync(boundedEnvelope);

    AssertTrue(!boundedResult.IsSuccess, "local process executor fails on non-zero exit code");
    AssertEqual("exit-code-7 stdout: 1234 [truncated]", boundedResult.Message, "local process executor bounds captured output");
}
finally
{
    processWorkDirectory.Delete(recursive: true);
}

Console.WriteLine("MediaIngest command runner tests passed.");

static MediaCommandEnvelope CreateEnvelope(ExecutionClass executionClass, string commandId)
{
    return new MediaCommandEnvelope(
        CommandId: commandId,
        CommandName: CommandNames.CreateProxy,
        TopicName: CommandNames.CreateProxy,
        ExecutionClass: executionClass,
        CommandLine: "ffmpeg -i source.mov proxy.mp4",
        WorkingDirectory: "/mnt/work/package-001",
        InputPaths: ["/mnt/ingest/package-001/source.mov"],
        OutputPaths: ["/mnt/work/package-001/proxy.mp4"],
        CorrelationId: $"correlation-{commandId}");
}

static ObservabilityCorrelationContext CreateCorrelation(MediaCommandEnvelope envelope)
{
    return new ObservabilityCorrelationContext(
        WorkflowInstanceId: "workflow-package-001",
        PackageId: "package-001",
        FileId: "file-source-001",
        WorkItemId: "work-item-proxy-001",
        NodeId: "node-proxy",
        AgentType: $"command-runner-{envelope.ExecutionClass.ToPropertyValue()}",
        QueueName: $"media.command.{envelope.ExecutionClass.ToPropertyValue()}",
        CorrelationId: envelope.CorrelationId,
        CausationId: envelope.CommandId,
        TraceId: "trace-001",
        SpanId: "span-001");
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true.");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertSequence<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    AssertEqual(expected.Count, actual.Count, $"{name} count");
    for (var index = 0; index < expected.Count; index++)
    {
        AssertEqual(expected[index], actual[index], $"{name} item {index}");
    }
}

static void AssertProgressCorrelation(
    ObservabilityCorrelationContext expectedCorrelation,
    MediaCommandEnvelope expectedEnvelope,
    CommandProgressRecord actual,
    string name)
{
    AssertEqual(expectedEnvelope.CommandId, actual.CommandId, $"{name} command id");
    AssertEqual(expectedEnvelope.CommandName, actual.CommandName, $"{name} command name");
    AssertEqual(expectedEnvelope.ExecutionClass, actual.ExecutionClass, $"{name} execution class");

    var fields = actual.CorrelationFields;
    AssertEqual(expectedCorrelation.WorkflowInstanceId, fields[CorrelationFieldNames.WorkflowInstanceId], $"{name} workflow correlation");
    AssertEqual(expectedCorrelation.PackageId, fields[CorrelationFieldNames.PackageId], $"{name} package correlation");
    AssertEqual(expectedCorrelation.FileId, fields[CorrelationFieldNames.FileId], $"{name} file correlation");
    AssertEqual(expectedCorrelation.WorkItemId, fields[CorrelationFieldNames.WorkItemId], $"{name} work item correlation");
    AssertEqual(expectedCorrelation.NodeId, fields[CorrelationFieldNames.NodeId], $"{name} node correlation");
    AssertEqual(expectedCorrelation.AgentType, fields[CorrelationFieldNames.AgentType], $"{name} agent correlation");
    AssertEqual(expectedCorrelation.QueueName, fields[CorrelationFieldNames.QueueName], $"{name} queue correlation");
    AssertEqual(expectedEnvelope.CorrelationId, fields[CorrelationFieldNames.CorrelationId], $"{name} command correlation");
    AssertEqual(expectedEnvelope.CommandId, fields[CorrelationFieldNames.CausationId], $"{name} command causation");
    AssertEqual(expectedCorrelation.TraceId, fields[CorrelationFieldNames.TraceId], $"{name} trace correlation");
    AssertEqual(expectedCorrelation.SpanId, fields[CorrelationFieldNames.SpanId], $"{name} span correlation");
}

internal sealed class RecordingCommandExecutor : ICommandExecutor
{
    public int ExecutionCount { get; private set; }

    public CommandExecutionResult NextResult { get; init; } = CommandExecutionResult.Succeeded();

    public Task<CommandExecutionResult> ExecuteAsync(MediaCommandEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        return Task.FromResult(NextResult);
    }
}

internal sealed class QueueingCommandExecutor(params CommandExecutionResult[] results) : ICommandExecutor
{
    private readonly Queue<CommandExecutionResult> results = new(results);

    public int ExecutionCount { get; private set; }

    public Task<CommandExecutionResult> ExecuteAsync(MediaCommandEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        return Task.FromResult(results.Dequeue());
    }
}

internal sealed class ThrowThenSucceedCommandExecutor : ICommandExecutor
{
    public int ExecutionCount { get; private set; }

    public Task<CommandExecutionResult> ExecuteAsync(MediaCommandEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        if (ExecutionCount == 1)
        {
            throw new InvalidOperationException("executor unavailable");
        }

        return Task.FromResult(CommandExecutionResult.Succeeded("retry-ok"));
    }
}
