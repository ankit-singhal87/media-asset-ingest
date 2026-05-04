using System.Text.Json;
using MediaIngest.Persistence;

namespace MediaIngest.Worker.Outbox;

public sealed record OutboxPublishRequest(
    OutboxMessage Message,
    IReadOnlyDictionary<string, string> ApplicationProperties)
{
    public const string ExecutionClassPropertyName = "executionClass";

    private static readonly HashSet<string> ExecutionClassValues = new(StringComparer.Ordinal)
    {
        "light",
        "medium",
        "heavy"
    };

    public static OutboxPublishRequest From(OutboxMessage message)
    {
        var applicationProperties = new Dictionary<string, string>(StringComparer.Ordinal);

        if (TryReadExecutionClass(message.PayloadJson, out var executionClass))
        {
            applicationProperties[ExecutionClassPropertyName] = executionClass;
        }
        else if (IsSemanticCommandTopic(message.Destination))
        {
            throw new InvalidOperationException(
                $"Command outbox message '{message.MessageId}' must include an executionClass value.");
        }

        return new OutboxPublishRequest(message, applicationProperties);
    }

    private static bool IsSemanticCommandTopic(string destination)
    {
        return destination.StartsWith("media.command.", StringComparison.Ordinal);
    }

    private static bool TryReadExecutionClass(string payloadJson, out string executionClass)
    {
        executionClass = string.Empty;

        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!TryGetProperty(root, "executionClass", out var executionClassProperty)
            && !TryGetProperty(root, "ExecutionClass", out executionClassProperty))
        {
            return false;
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

        var normalizedValue = value.ToLowerInvariant();

        if (!ExecutionClassValues.Contains(normalizedValue))
        {
            throw new InvalidOperationException("Command executionClass must be light, medium, or heavy.");
        }

        executionClass = normalizedValue;
        return true;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        return element.TryGetProperty(propertyName, out property);
    }
}
