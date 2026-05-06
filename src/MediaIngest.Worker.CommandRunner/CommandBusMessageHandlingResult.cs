namespace MediaIngest.Worker.CommandRunner;

public sealed record CommandBusMessageHandlingResult(
    CommandBusMessageDisposition Disposition,
    string MessageId,
    string? CommandId,
    string Reason)
{
    public static CommandBusMessageHandlingResult Complete(string messageId, string commandId, string reason)
    {
        return new CommandBusMessageHandlingResult(
            CommandBusMessageDisposition.Complete,
            messageId,
            commandId,
            reason);
    }

    public static CommandBusMessageHandlingResult Abandon(string messageId, string? commandId, string reason)
    {
        return new CommandBusMessageHandlingResult(
            CommandBusMessageDisposition.Abandon,
            messageId,
            commandId,
            reason);
    }

    public static CommandBusMessageHandlingResult DeadLetter(string messageId, string? commandId, string reason)
    {
        return new CommandBusMessageHandlingResult(
            CommandBusMessageDisposition.DeadLetter,
            messageId,
            commandId,
            reason);
    }
}
