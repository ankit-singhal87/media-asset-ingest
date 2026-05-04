namespace MediaIngest.Worker.CommandRunner;

public enum CommandProgressStatus
{
    Accepted,
    Succeeded,
    Failed,
    Rejected,
    DuplicateSkipped
}
