namespace MediaIngest.Contracts.Commands;

public static class CommandNames
{
    public const string CreateProxy = "media.command.create_proxy";
    public const string CreateChecksum = "media.command.create_checksum";
    public const string VerifyChecksum = "media.command.verify_checksum";
    public const string RunSecurityScan = "media.command.run_security_scan";
    public const string ArchiveAsset = "media.command.archive_asset";
}
