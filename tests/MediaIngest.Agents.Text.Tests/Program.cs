using MediaIngest.Agents.Text;

AssertEqual("text", TextAgentWorker.AssignedCategory, "assigned category");
AssertTrue(TextAgentWorker.OwnsCategory("text"), "text agent should own text work");
AssertFalse(TextAgentWorker.OwnsCategory("video"), "text agent should not own video work");
AssertFalse(TextAgentWorker.OwnsCategory("audio"), "text agent should not own audio work");
AssertFalse(TextAgentWorker.OwnsCategory("other"), "text agent should not own other work");

Console.WriteLine("MediaIngest text agent ownership smoke test passed.");

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
