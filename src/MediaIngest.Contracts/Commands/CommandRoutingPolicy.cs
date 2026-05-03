namespace MediaIngest.Contracts.Commands;

public static class CommandRoutingPolicy
{
    public const long LightMaxBytesExclusive = 512L * 1024L * 1024L;
    public const long HeavyMinBytesExclusive = 10L * 1024L * 1024L * 1024L;

    public static CommandRoute Route(string commandName, long inputBytes)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            throw new ArgumentException("Command name is required.", nameof(commandName));
        }

        if (inputBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inputBytes), "Input bytes cannot be negative.");
        }

        return new CommandRoute(
            TopicName: commandName,
            ExecutionClass: SelectExecutionClass(commandName, inputBytes));
    }

    private static ExecutionClass SelectExecutionClass(string commandName, long inputBytes)
    {
        if (string.Equals(commandName, CommandNames.CreateChecksum, StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, CommandNames.VerifyChecksum, StringComparison.OrdinalIgnoreCase)
            || string.Equals(commandName, CommandNames.RunSecurityScan, StringComparison.OrdinalIgnoreCase))
        {
            return inputBytes < LightMaxBytesExclusive ? ExecutionClass.Light : ExecutionClass.Medium;
        }

        if (inputBytes < LightMaxBytesExclusive)
        {
            return ExecutionClass.Light;
        }

        if (inputBytes <= HeavyMinBytesExclusive)
        {
            return ExecutionClass.Medium;
        }

        return ExecutionClass.Heavy;
    }
}
