namespace MediaIngest.Essence.Checksums;

public enum ManifestChecksumFailureReason
{
    None = 0,
    ManifestFileMissing,
    ChecksumFileMissing,
    MalformedChecksum,
    ChecksumMismatch
}
