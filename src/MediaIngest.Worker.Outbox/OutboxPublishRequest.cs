using System.Text.Json;
using MediaIngest.Contracts.Commands;
using MediaIngest.Persistence;

namespace MediaIngest.Worker.Outbox;

public sealed record OutboxPublishRequest(
    OutboxMessage Message,
    IReadOnlyDictionary<string, string> ApplicationProperties)
{
    public static OutboxPublishRequest From(OutboxMessage message)
    {
        var applicationProperties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (IsMediaCommandEnvelope(message))
        {
            applicationProperties[CommandRoute.ExecutionClassPropertyName] = ReadExecutionClass(message);
        }

        return new OutboxPublishRequest(message, applicationProperties);
    }

    private static bool IsMediaCommandEnvelope(OutboxMessage message)
    {
        return string.Equals(message.MessageType, nameof(MediaCommandEnvelope), StringComparison.Ordinal);
    }

    private static string ReadExecutionClass(OutboxMessage message)
    {
        using var document = JsonDocument.Parse(message.PayloadJson);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Command outbox message '{message.MessageId}' must contain a JSON object payload.");
        }

        if (!TryGetProperty(root, "executionClass", out var executionClassProperty)
            && !TryGetProperty(root, "ExecutionClass", out executionClassProperty))
        {
            throw new InvalidOperationException(
                $"Command outbox message '{message.MessageId}' must include an executionClass value.");
        }

        if (executionClassProperty.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("Command executionClass must be a string.");
        }

        var value = executionClassProperty.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Command executionClass is required.");
        }

        try
        {
            return ExecutionClassProperties.FromPropertyValue(value).ToPropertyValue();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidOperationException("Command executionClass must be light, medium, or heavy.", ex);
        }
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        return element.TryGetProperty(propertyName, out property);
    }
}
