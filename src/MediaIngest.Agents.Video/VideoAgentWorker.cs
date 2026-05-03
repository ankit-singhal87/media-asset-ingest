namespace MediaIngest.Agents.Video;

public static class VideoAgentWorker
{
    public const string AssignedCategory = "video";

    public static bool OwnsCategory(string mediaCategory)
    {
        return string.Equals(mediaCategory, AssignedCategory, StringComparison.OrdinalIgnoreCase);
    }
}
