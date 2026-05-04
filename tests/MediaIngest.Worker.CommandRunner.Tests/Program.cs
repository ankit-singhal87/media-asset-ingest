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

var failedExecutor = new RecordingCommandExecutor { NextResult = CommandExecutionResult.Failed("exit-code-1") };
var failedProgress = new InMemoryCommandProgressSink();
var failedRunner = new GenericCommandRunner(ExecutionClass.Heavy, failedExecutor, failedProgress);
var failedEnvelope = CreateEnvelope(ExecutionClass.Heavy, "command-failed");

var failedResult = await failedRunner.HandleAsync(failedEnvelope, CreateCorrelation(failedEnvelope));

AssertEqual(CommandHandlingStatus.Failed, failedResult.Status, "failed command returns failed status");
AssertEqual("exit-code-1", failedResult.Message, "failed command preserves failure message");
AssertEqual(CommandProgressStatus.Failed, failedProgress.Records.Last().Status, "failed command is recorded");

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
AssertEqual(expectedCorrelation.WorkItemId, progressFields[CorrelationFieldNames.WorkItemId], "work item correlation field");
AssertEqual(expectedCorrelation.NodeId, progressFields[CorrelationFieldNames.NodeId], "node correlation field");
AssertEqual(expectedCorrelation.AgentType, progressFields[CorrelationFieldNames.AgentType], "agent type correlation field");
AssertEqual(expectedCorrelation.QueueName, progressFields[CorrelationFieldNames.QueueName], "queue correlation field");
AssertEqual(correlationEnvelope.CorrelationId, progressFields[CorrelationFieldNames.CorrelationId], "command correlation field");
AssertEqual(correlationEnvelope.CommandId, progressFields[CorrelationFieldNames.CausationId], "command causation field");

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
