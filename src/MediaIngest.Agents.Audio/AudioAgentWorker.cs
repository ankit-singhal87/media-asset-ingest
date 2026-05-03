namespace MediaIngest.Agents.Audio;

public static class AudioAgentWorker
{
    public const string AssignedCategory = "audio";

    public static bool OwnsCategory(string mediaCategory)
    {
        return string.Equals(mediaCategory, AssignedCategory, StringComparison.OrdinalIgnoreCase);
    }
}
