using MediaIngest.Worker.Watcher;

var mountPath = Path.Combine(Path.GetTempPath(), "media-ingest-watcher-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(mountPath);

try
{
    var firstPackagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-a")).FullName;
    var secondPackagePath = Directory.CreateDirectory(Path.Combine(mountPath, "package-b")).FullName;
    File.WriteAllText(Path.Combine(mountPath, "loose-file.txt"), "not a package");

    var scanner = new IngestMountScanner();

    var candidates = scanner.FindPackageCandidates(mountPath);

    AssertEqual(2, candidates.Count, "package candidate count");
    AssertSequenceEqual(
        [firstPackagePath, secondPackagePath],
        candidates.Select(candidate => candidate.PackagePath).ToArray(),
        "package candidate paths");
}
finally
{
    if (Directory.Exists(mountPath))
    {
        Directory.Delete(mountPath, recursive: true);
    }
}

Console.WriteLine("MediaIngest watcher smoke tests passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string name)
{
    AssertEqual(expected.Count, actual.Count, $"{name} length");

    for (var index = 0; index < expected.Count; index++)
    {
        AssertEqual(expected[index], actual[index], $"{name} item {index}");
    }
}
