using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed partial class CallbackTemplateRenderer
{
    private static readonly IReadOnlySet<string> AllowedTokens = new HashSet<string>(
        ["eventType", "isFile", "targetEventSourcePath", "timestamp"],
        StringComparer.Ordinal);

    public void Validate(string callbackUrlTemplate, string callbackPayloadTemplate, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callbackUrlTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(callbackPayloadTemplate);

        foreach (var token in FindTokens(callbackUrlTemplate).Concat(FindTokens(callbackPayloadTemplate)))
        {
            if (!AllowedTokens.Contains(token))
            {
                throw new InvalidOperationException($"{label} contains unsupported callback token '{{{token}}}'.");
            }
        }
    }

    public string Render(string template, CallbackTemplateValues values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        Validate(template, "{}", "callback template");

        return template
            .Replace("{eventType}", values.EventType, StringComparison.Ordinal)
            .Replace("{isFile}", values.IsFile.ToString().ToLowerInvariant(), StringComparison.Ordinal)
            .Replace("{targetEventSourcePath}", values.TargetEventSourcePath, StringComparison.Ordinal)
            .Replace("{timestamp}", values.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    public string RenderPayloadJson(string template, CallbackTemplateValues values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        Validate("https://callback.test/{eventType}", template, "callback payload template");

        return template
            .Replace("{eventType}", EncodeJsonValue(values.EventType), StringComparison.Ordinal)
            .Replace("{isFile}", EncodeJsonValue(values.IsFile.ToString().ToLowerInvariant()), StringComparison.Ordinal)
            .Replace("{targetEventSourcePath}", EncodeJsonValue(values.TargetEventSourcePath), StringComparison.Ordinal)
            .Replace(
                "{timestamp}",
                EncodeJsonValue(values.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                StringComparison.Ordinal);
    }

    private static string EncodeJsonValue(string value)
    {
        return JsonEncodedText.Encode(value, JavaScriptEncoder.Default).ToString();
    }

    private static IEnumerable<string> FindTokens(string template)
    {
        foreach (Match match in CallbackTokenRegex().Matches(template))
        {
            yield return match.Groups["token"].Value;
        }
    }

    [GeneratedRegex(@"\{(?<token>[^{}]+)\}")]
    private static partial Regex CallbackTokenRegex();
}
