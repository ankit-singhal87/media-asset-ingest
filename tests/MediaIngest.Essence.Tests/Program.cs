using System.Security.Cryptography;
using System.Text;
using MediaIngest.Essence.Checksums;

var workspace = Path.Combine(Path.GetTempPath(), "media-ingest-essence-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(workspace);

try
{
    var manifestPath = Path.Combine(workspace, "manifest.json");
    var checksumPath = Path.Combine(workspace, "manifest.json.checksum");
    File.WriteAllText(manifestPath, """{"packageId":"asset-001"}""");

    var expectedChecksum = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifestPath))).ToLowerInvariant();
    File.WriteAllText(checksumPath, expectedChecksum);

    var validResult = ManifestChecksumValidator.Validate(manifestPath, checksumPath);
    AssertTrue(validResult.IsValid, "valid checksum succeeds");
    AssertEqual(ManifestChecksumFailureReason.None, validResult.FailureReason, "valid checksum reason");
    AssertEqual(expectedChecksum, validResult.ExpectedChecksumHex, "expected checksum is normalized");
    AssertEqual(expectedChecksum, validResult.ActualChecksumHex, "actual checksum is computed");

    var uppercaseChecksumPath = Path.Combine(workspace, "manifest-uppercase.json.checksum");
    File.WriteAllText(uppercaseChecksumPath, $"  {expectedChecksum.ToUpperInvariant()}{Environment.NewLine}");

    var uppercaseResult = ManifestChecksumValidator.Validate(manifestPath, uppercaseChecksumPath);
    AssertTrue(uppercaseResult.IsValid, "uppercase checksum succeeds");
    AssertEqual(expectedChecksum, uppercaseResult.ExpectedChecksumHex, "uppercase checksum is normalized");

    var missingResult = ManifestChecksumValidator.Validate(
        manifestPath,
        Path.Combine(workspace, "missing-manifest.json.checksum"));
    AssertFalse(missingResult.IsValid, "missing checksum fails");
    AssertEqual(ManifestChecksumFailureReason.ChecksumFileMissing, missingResult.FailureReason, "missing checksum reason");

    File.WriteAllText(checksumPath, "not-a-sha256");
    var malformedResult = ManifestChecksumValidator.Validate(manifestPath, checksumPath);
    AssertFalse(malformedResult.IsValid, "malformed checksum fails");
    AssertEqual(ManifestChecksumFailureReason.MalformedChecksum, malformedResult.FailureReason, "malformed checksum reason");

    File.WriteAllText(checksumPath, new string('0', 64));
    var mismatchedResult = ManifestChecksumValidator.Validate(manifestPath, checksumPath);
    AssertFalse(mismatchedResult.IsValid, "mismatched checksum fails");
    AssertEqual(ManifestChecksumFailureReason.ChecksumMismatch, mismatchedResult.FailureReason, "mismatched checksum reason");
    AssertEqual(new string('0', 64), mismatchedResult.ExpectedChecksumHex, "mismatched expected checksum");
    AssertEqual(expectedChecksum, mismatchedResult.ActualChecksumHex, "mismatched actual checksum");

    Console.WriteLine("MediaIngest essence checksum tests passed.");
}
finally
{
    Directory.Delete(workspace, recursive: true);
}

static void AssertTrue(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException($"{name}: expected true.");
    }
}

static void AssertFalse(bool condition, string name)
{
    if (condition)
    {
        throw new InvalidOperationException($"{name}: expected false.");
    }
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}
