using System.Text.Json;

namespace MediaIngest.Worker.Watcher;

internal sealed class ManifestReferenceReader
{
    public ManifestReferences ReadFileReferences(string manifestPath)
    {
        try
        {
            using var manifest = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(manifest);

            if (!document.RootElement.TryGetProperty("files", out var filesElement) ||
                filesElement.ValueKind != JsonValueKind.Array)
            {
                return ManifestReferences.Empty;
            }

            var filePaths = filesElement
                .EnumerateArray()
                .Where(fileElement => fileElement.ValueKind == JsonValueKind.String)
                .Select(fileElement => fileElement.GetString())
                .Where(filePath => !string.IsNullOrWhiteSpace(filePath))
                .Select(NormalizeManifestPath)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            return new ManifestReferences(filePaths, MalformedWarning: null);
        }
        catch (JsonException exception)
        {
            return new ManifestReferences(
                [],
                new IngestPackageWarning(
                    "ManifestMalformed",
                    "manifest.json",
                    $"Manifest could not be parsed: {exception.Message}"));
        }
    }

    private static string NormalizeManifestPath(string? filePath)
    {
        return filePath!
            .Trim()
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
    }
}

internal sealed record ManifestReferences(
    IReadOnlyList<string> FilePaths,
    IngestPackageWarning? MalformedWarning)
{
    public static ManifestReferences Empty { get; } = new([], MalformedWarning: null);
}
