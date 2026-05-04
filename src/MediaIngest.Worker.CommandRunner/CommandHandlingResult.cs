namespace MediaIngest.Worker.CommandRunner;

public sealed record CommandHandlingResult(CommandHandlingStatus Status, string? Message)
{
    public static CommandHandlingResult Succeeded(string? message = null)
    {
        return new CommandHandlingResult(CommandHandlingStatus.Succeeded, message);
    }

    public static CommandHandlingResult RejectedExecutionClass(string message)
    {
        return new CommandHandlingResult(CommandHandlingStatus.RejectedExecutionClass, message);
    }

    public static CommandHandlingResult Duplicate(string message)
    {
        return new CommandHandlingResult(CommandHandlingStatus.Duplicate, message);
    }

    public static CommandHandlingResult Failed(string message)
    {
        return new CommandHandlingResult(CommandHandlingStatus.Failed, message);
    }
}
