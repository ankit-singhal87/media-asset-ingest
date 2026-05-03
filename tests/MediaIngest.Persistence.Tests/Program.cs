using MediaIngest.Persistence;

var store = new InMemoryIngestPersistenceStore();

var packageState = new IngestPackageState(
    PackageId: "package-001",
    WorkflowInstanceId: "workflow-package-001",
    Status: "WorkAccepted",
    UpdatedAt: new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.Zero));

var command = new OutboxMessage(
    MessageId: "message-001",
    Destination: "media-ingest.video",
    MessageType: "ProcessVideo",
    PayloadJson: """{"packageId":"package-001","filePath":"video/source.mov"}""",
    CorrelationId: "correlation-001",
    CreatedAt: packageState.UpdatedAt);

await store.SaveAsync(new PersistenceBatch([packageState], [command]));

AssertEqual(1, store.PackageStates.Count, "saved business state count");
AssertEqual(1, store.OutboxMessages.Count, "saved outbox message count");
AssertEqual("package-001", store.PackageStates[0].PackageId, "business state package id");
AssertEqual("message-001", store.OutboxMessages[0].MessageId, "outbox message id");

var rejected = false;

try
{
    await store.SaveAsync(new PersistenceBatch(
        [packageState with { PackageId = "package-002" }],
        [command with { MessageId = "" }]));
}
catch (ArgumentException)
{
    rejected = true;
}

AssertTrue(rejected, "invalid outbox message rejects the persistence batch");
AssertEqual(1, store.PackageStates.Count, "business state count after rejected batch");
AssertEqual("package-001", store.PackageStates[0].PackageId, "business state after rejected batch");
AssertEqual(1, store.OutboxMessages.Count, "outbox count after rejected batch");

Console.WriteLine("MediaIngest persistence boundary smoke tests passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true.");
    }
}
