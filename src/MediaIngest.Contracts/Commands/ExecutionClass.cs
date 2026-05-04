using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaIngest.Contracts.Commands;

[JsonConverter(typeof(ExecutionClassJsonConverter))]
public enum ExecutionClass
{
    Light,
    Medium,
    Heavy
}

public static class ExecutionClassProperties
{
    public const string Light = "light";
    public const string Medium = "medium";
    public const string Heavy = "heavy";

    public static string ToPropertyValue(this ExecutionClass executionClass)
    {
        return executionClass switch
        {
            ExecutionClass.Light => Light,
            ExecutionClass.Medium => Medium,
            ExecutionClass.Heavy => Heavy,
            _ => throw new ArgumentOutOfRangeException(nameof(executionClass), executionClass, "Unknown execution class.")
        };
    }

    public static ExecutionClass FromPropertyValue(string value)
    {
        return value.ToLowerInvariant() switch
        {
            Light => ExecutionClass.Light,
            Medium => ExecutionClass.Medium,
            Heavy => ExecutionClass.Heavy,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown execution class.")
        };
    }
}

public sealed class ExecutionClassJsonConverter : JsonConverter<ExecutionClass>
{
    public override ExecutionClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Execution class must be a string.");
        }

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Execution class is required.");
        }

        try
        {
            return ExecutionClassProperties.FromPropertyValue(value);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new JsonException("Execution class must be light, medium, or heavy.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, ExecutionClass value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToPropertyValue());
    }
}
