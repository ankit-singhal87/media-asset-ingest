using MediaIngest.Agents.Other;

AssertEqual("other", OtherAgentWorker.AssignedCategory, "assigned category");
AssertTrue(OtherAgentWorker.OwnsCategory("other"), "other agent should own other work");
AssertFalse(OtherAgentWorker.OwnsCategory("video"), "other agent should not own video work");
AssertFalse(OtherAgentWorker.OwnsCategory("audio"), "other agent should not own audio work");
AssertFalse(OtherAgentWorker.OwnsCategory("text"), "other agent should not own text work");

Console.WriteLine("MediaIngest other agent ownership smoke test passed.");

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
