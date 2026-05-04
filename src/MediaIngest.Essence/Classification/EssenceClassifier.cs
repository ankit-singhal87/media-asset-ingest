namespace MediaIngest.Essence.Classification;

public static class EssenceClassifier
{
    private static readonly HashSet<string> VideoSourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mov",
        ".mxf",
        ".mp4",
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".srt",
        ".txt",
        ".vtt",
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".wav",
    };

    public static EssenceType Classify(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var extension = Path.GetExtension(filePath);

        if (VideoSourceExtensions.Contains(extension))
        {
            return EssenceType.VideoSource;
        }

        if (TextExtensions.Contains(extension))
        {
            return EssenceType.Text;
        }

        if (AudioExtensions.Contains(extension))
        {
            return EssenceType.Audio;
        }

        return EssenceType.Other;
    }
}
