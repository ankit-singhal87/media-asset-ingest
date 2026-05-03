using MediaIngest.Agents.Video;

AssertEqual("video", VideoAgentWorker.AssignedCategory, "assigned category");
AssertTrue(VideoAgentWorker.OwnsCategory("video"), "video agent should own video work");
AssertFalse(VideoAgentWorker.OwnsCategory("audio"), "video agent should not own audio work");
AssertFalse(VideoAgentWorker.OwnsCategory("text"), "video agent should not own text work");
AssertFalse(VideoAgentWorker.OwnsCategory("other"), "video agent should not own other work");

Console.WriteLine("MediaIngest video agent ownership smoke test passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
{
    if (condition)
    {
        throw new InvalidOperationException(message);
    }
}
