using System.Security.Cryptography;

namespace MediaIngest.Essence.Checksums;

public static class ManifestChecksumValidator
{
    private const int Sha256HexLength = 64;

    public static ManifestChecksumValidationResult Validate(string manifestPath, string checksumPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(checksumPath);

        if (!File.Exists(manifestPath))
        {
            return ManifestChecksumValidationResult.Failed(ManifestChecksumFailureReason.ManifestFileMissing);
        }

        if (!File.Exists(checksumPath))
        {
            return ManifestChecksumValidationResult.Failed(ManifestChecksumFailureReason.ChecksumFileMissing);
        }

        var expectedChecksumHex = File.ReadAllText(checksumPath).Trim();
        if (!IsSha256Hex(expectedChecksumHex))
        {
            return ManifestChecksumValidationResult.Failed(ManifestChecksumFailureReason.MalformedChecksum);
        }

        expectedChecksumHex = expectedChecksumHex.ToLowerInvariant();
        var actualChecksumHex = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifestPath))).ToLowerInvariant();

        return string.Equals(expectedChecksumHex, actualChecksumHex, StringComparison.Ordinal)
            ? ManifestChecksumValidationResult.Valid(actualChecksumHex)
            : ManifestChecksumValidationResult.Failed(
                ManifestChecksumFailureReason.ChecksumMismatch,
                expectedChecksumHex,
                actualChecksumHex);
    }

    private static bool IsSha256Hex(string value)
    {
        if (value.Length != Sha256HexLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
