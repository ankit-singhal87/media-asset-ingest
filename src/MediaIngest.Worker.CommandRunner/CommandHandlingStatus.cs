namespace MediaIngest.Worker.CommandRunner;

public enum CommandHandlingStatus
{
    Succeeded,
    RejectedExecutionClass,
    Duplicate,
    Failed
}
