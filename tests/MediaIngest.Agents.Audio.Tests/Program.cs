using MediaIngest.Agents.Audio;

AssertEqual("audio", AudioAgentWorker.AssignedCategory, "assigned category");
AssertTrue(AudioAgentWorker.OwnsCategory("audio"), "audio agent should own audio work");
AssertFalse(AudioAgentWorker.OwnsCategory("video"), "audio agent should not own video work");
AssertFalse(AudioAgentWorker.OwnsCategory("text"), "audio agent should not own text work");
AssertFalse(AudioAgentWorker.OwnsCategory("other"), "audio agent should not own other work");

Console.WriteLine("MediaIngest audio agent ownership smoke test passed.");

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
