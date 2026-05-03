namespace MediaIngest.Agents.Other;

public static class OtherAgentWorker
{
    public const string AssignedCategory = "other";

    public static bool OwnsCategory(string mediaCategory)
    {
        return string.Equals(mediaCategory, AssignedCategory, StringComparison.OrdinalIgnoreCase);
    }
}
