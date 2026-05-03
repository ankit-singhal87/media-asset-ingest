namespace MediaIngest.Essence.Checksums;

public sealed record ManifestChecksumValidationResult(
    bool IsValid,
    ManifestChecksumFailureReason FailureReason,
    string? ExpectedChecksumHex,
    string? ActualChecksumHex)
{
    public static ManifestChecksumValidationResult Valid(string checksumHex) =>
        new(
            IsValid: true,
            FailureReason: ManifestChecksumFailureReason.None,
            ExpectedChecksumHex: checksumHex,
            ActualChecksumHex: checksumHex);

    public static ManifestChecksumValidationResult Failed(
        ManifestChecksumFailureReason failureReason,
        string? expectedChecksumHex = null,
        string? actualChecksumHex = null) =>
        new(
            IsValid: false,
            FailureReason: failureReason,
            ExpectedChecksumHex: expectedChecksumHex,
            ActualChecksumHex: actualChecksumHex);
}
