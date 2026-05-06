using System.Text.Json;
using MediaIngest.Contracts.Commands;
using MediaIngest.Observability;

namespace MediaIngest.Worker.CommandRunner;

public sealed class CommandBusMessageConsumer
{
    private readonly GenericCommandRunner runner;

    public CommandBusMessageConsumer(GenericCommandRunner runner)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public async Task<CommandBusMessageHandlingResult> HandleAsync(
        CommandBusReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        string? commandId = null;
        try
        {
            var routeValidation = ValidateBrokerRoute(message);
            if (routeValidation is not null)
            {
                return routeValidation;
            }

            var envelopeResult = DeserializeEnvelope(message);
            if (envelopeResult.Result is not null)
            {
                return envelopeResult.Result;
            }

            var envelope = envelopeResult.Envelope;
            if (envelope is null)
            {
                return CommandBusMessageHandlingResult.DeadLetter(
                    message.MessageId,
                    null,
                    "Command message body is empty.");
            }

            var envelopeShapeValidation = ValidateEnvelopeShape(
                message,
                envelope,
                envelopeResult.HasExecutionClassProperty);
            if (envelopeShapeValidation is not null)
            {
                return envelopeShapeValidation;
            }

            commandId = envelope.CommandId;

            var envelopeValidation = ValidateEnvelopeRoute(message, envelope);
            if (envelopeValidation is not null)
            {
                return envelopeValidation;
            }

            var correlation = CreateCorrelation(message, envelope);
            var handling = await runner.HandleAsync(envelope, correlation, cancellationToken).ConfigureAwait(false);
            var reason = handling.Message ?? $"Command handling finished with {handling.Status}.";
            return handling.Status switch
            {
                CommandHandlingStatus.Succeeded => CommandBusMessageHandlingResult.Complete(
                    message.MessageId,
                    envelope.CommandId,
                    reason),
                CommandHandlingStatus.Duplicate => CommandBusMessageHandlingResult.Complete(
                    message.MessageId,
                    envelope.CommandId,
                    reason),
                CommandHandlingStatus.Failed => CommandBusMessageHandlingResult.Abandon(
                    message.MessageId,
                    envelope.CommandId,
                    reason),
                CommandHandlingStatus.RejectedExecutionClass => CommandBusMessageHandlingResult.DeadLetter(
                    message.MessageId,
                    envelope.CommandId,
                    reason),
                _ => CommandBusMessageHandlingResult.Abandon(
                    message.MessageId,
                    envelope.CommandId,
                    $"Unhandled command status {handling.Status}.")
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CommandBusMessageHandlingResult.Abandon(message.MessageId, commandId, ex.Message);
        }
    }

    private static CommandBusMessageHandlingResult? ValidateBrokerRoute(CommandBusReceivedMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.MessageId))
        {
            return CommandBusMessageHandlingResult.DeadLetter(string.Empty, null, "MessageId is required.");
        }

        if (!CommandBusTopology.CommandTopics.Contains(message.TopicName, StringComparer.Ordinal))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                null,
                $"Unknown command topic '{message.TopicName}'.");
        }

        var subscription = CommandBusTopology.Subscriptions.SingleOrDefault(candidate =>
            string.Equals(candidate.SubscriptionName, message.SubscriptionName, StringComparison.Ordinal));
        if (subscription is null)
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                null,
                $"Unknown command subscription '{message.SubscriptionName}'.");
        }

        if (!message.ApplicationProperties.TryGetValue(
                CommandRoute.ExecutionClassPropertyName,
                out var executionClassProperty))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                null,
                $"Missing {CommandRoute.ExecutionClassPropertyName} application property.");
        }

        ExecutionClass executionClass;
        try
        {
            executionClass = ExecutionClassProperties.FromPropertyValue(executionClassProperty);
        }
        catch (ArgumentException ex)
        {
            return CommandBusMessageHandlingResult.DeadLetter(message.MessageId, null, ex.Message);
        }

        var normalizedExecutionClass = executionClass.ToPropertyValue();
        if (!string.Equals(subscription.FilterPropertyValue, normalizedExecutionClass, StringComparison.Ordinal))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                null,
                $"Subscription '{message.SubscriptionName}' does not match execution class '{normalizedExecutionClass}'.");
        }

        return null;
    }

    private static EnvelopeReadResult DeserializeEnvelope(CommandBusReceivedMessage message)
    {
        try
        {
            using var document = JsonDocument.Parse(message.BodyJson);
            var hasExecutionClassProperty = HasProperty(
                document.RootElement,
                nameof(MediaCommandEnvelope.ExecutionClass));
            var envelope = document.RootElement.Deserialize<MediaCommandEnvelope>();
            return envelope is null
                ? new EnvelopeReadResult(
                    null,
                    hasExecutionClassProperty,
                    CommandBusMessageHandlingResult.DeadLetter(
                        message.MessageId,
                        null,
                        "Command message body is empty."))
                : new EnvelopeReadResult(envelope, hasExecutionClassProperty, null);
        }
        catch (JsonException ex)
        {
            return new EnvelopeReadResult(
                null,
                HasExecutionClassProperty: false,
                CommandBusMessageHandlingResult.DeadLetter(
                    message.MessageId,
                    null,
                    $"Command message body is not valid JSON: {ex.Message}"));
        }
    }

    private static CommandBusMessageHandlingResult? ValidateEnvelopeShape(
        CommandBusReceivedMessage message,
        MediaCommandEnvelope envelope,
        bool hasExecutionClassProperty)
    {
        if (string.IsNullOrWhiteSpace(envelope.CommandId))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope CommandId is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.CommandName))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope CommandName is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.TopicName))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope TopicName is required.");
        }

        if (!hasExecutionClassProperty)
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope ExecutionClass is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.CommandLine))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope CommandLine is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.WorkingDirectory))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope WorkingDirectory is required.");
        }

        if (envelope.InputPaths is null)
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope InputPaths is required.");
        }

        if (envelope.OutputPaths is null)
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope OutputPaths is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.CorrelationId))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                "Envelope CorrelationId is required.");
        }

        return null;
    }

    private CommandBusMessageHandlingResult? ValidateEnvelopeRoute(
        CommandBusReceivedMessage message,
        MediaCommandEnvelope envelope)
    {
        if (!string.Equals(envelope.TopicName, message.TopicName, StringComparison.Ordinal))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                $"Envelope topic '{envelope.TopicName}' does not match broker topic '{message.TopicName}'.");
        }

        if (!string.Equals(envelope.CommandName, envelope.TopicName, StringComparison.Ordinal))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                $"Envelope command name '{envelope.CommandName}' does not match envelope topic '{envelope.TopicName}'.");
        }

        var envelopeExecutionClass = envelope.ExecutionClass.ToPropertyValue();
        var propertyExecutionClass = ExecutionClassProperties
            .FromPropertyValue(message.ApplicationProperties[CommandRoute.ExecutionClassPropertyName])
            .ToPropertyValue();
        if (!string.Equals(envelopeExecutionClass, propertyExecutionClass, StringComparison.Ordinal))
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                $"Envelope execution class '{envelopeExecutionClass}' does not match broker execution class '{propertyExecutionClass}'.");
        }

        if (envelope.ExecutionClass != runner.ConfiguredExecutionClass)
        {
            return CommandBusMessageHandlingResult.DeadLetter(
                message.MessageId,
                envelope.CommandId,
                $"Runner configured for {runner.ConfiguredExecutionClass.ToPropertyValue()} cannot consume {envelopeExecutionClass} subscription messages.");
        }

        return null;
    }

    private static ObservabilityCorrelationContext CreateCorrelation(
        CommandBusReceivedMessage message,
        MediaCommandEnvelope envelope)
    {
        var executionClass = envelope.ExecutionClass.ToPropertyValue();
        return new ObservabilityCorrelationContext(
            WorkflowInstanceId: envelope.CorrelationId,
            PackageId: envelope.WorkingDirectory,
            FileId: envelope.InputPaths.FirstOrDefault() ?? envelope.CommandId,
            WorkItemId: envelope.CommandId,
            NodeId: envelope.CommandName,
            AgentType: $"command-runner-{executionClass}",
            QueueName: $"{message.TopicName}/{message.SubscriptionName}",
            CorrelationId: envelope.CorrelationId,
            CausationId: message.MessageId,
            TraceId: envelope.CorrelationId,
            SpanId: envelope.CommandId);
    }

    private static bool HasProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return element
            .EnumerateObject()
            .Any(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal));
    }

    private sealed record EnvelopeReadResult(
        MediaCommandEnvelope? Envelope,
        bool HasExecutionClassProperty,
        CommandBusMessageHandlingResult? Result);
}
