namespace MediaIngest.Agents.Text;

public static class TextAgentWorker
{
    public const string AssignedCategory = "text";

    public static bool OwnsCategory(string mediaCategory)
    {
        return string.Equals(mediaCategory, AssignedCategory, StringComparison.OrdinalIgnoreCase);
    }
}
