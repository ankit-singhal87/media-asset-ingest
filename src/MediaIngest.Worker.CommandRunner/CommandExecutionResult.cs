namespace MediaIngest.Worker.CommandRunner;

public sealed record CommandExecutionResult(bool IsSuccess, string? Message)
{
    public static CommandExecutionResult Succeeded(string? message = null)
    {
        return new CommandExecutionResult(IsSuccess: true, message);
    }

    public static CommandExecutionResult Failed(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new CommandExecutionResult(IsSuccess: false, message);
    }
}
