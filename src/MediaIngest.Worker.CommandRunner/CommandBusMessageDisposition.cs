namespace MediaIngest.Worker.CommandRunner;

public enum CommandBusMessageDisposition
{
    Complete,
    Abandon,
    DeadLetter
}
