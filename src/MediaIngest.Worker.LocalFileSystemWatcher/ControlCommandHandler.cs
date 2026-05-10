using Microsoft.EntityFrameworkCore;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class ControlCommandHandler(
    WatcherDbContext context,
    CallbackTemplateRenderer templateRenderer,
    TimeProvider timeProvider,
    IReconciliationSignal? reconciliationSignal = null)
{
    public async Task<ControlCommand> HandleAsync(
        string commandId,
        string commandType,
        WatchDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentNullException.ThrowIfNull(definition);

        var existingCommand = await context.ControlCommands.FindAsync([commandId], cancellationToken);
        if (existingCommand is not null)
        {
            return new ControlCommand
            {
                CommandId = commandId,
                WatchId = definition.WatchId,
                CommandType = commandType,
                ReceivedAt = existingCommand.ReceivedAt,
                AppliedAt = existingCommand.AppliedAt,
                Result = "duplicate"
            };
        }

        templateRenderer.Validate(
            definition.CallbackUrlTemplate,
            definition.CallbackPayloadTemplate,
            $"watch {definition.WatchId}");

        var now = timeProvider.GetUtcNow();
        var watch = await context.Watches.SingleOrDefaultAsync(
            candidate => candidate.WatchId == definition.WatchId,
            cancellationToken);

        var result = ApplyCommand(commandType, definition, watch, now);
        var command = new ControlCommand
        {
            CommandId = commandId,
            WatchId = definition.WatchId,
            CommandType = commandType,
            ReceivedAt = now,
            AppliedAt = now,
            Result = result
        };

        context.ControlCommands.Add(command);
        await context.SaveChangesAsync(cancellationToken);
        reconciliationSignal?.RequestReconciliation();

        return command;
    }

    private string ApplyCommand(
        string commandType,
        WatchDefinition definition,
        Watch? watch,
        DateTimeOffset now)
    {
        switch (commandType)
        {
            case "create_watcher":
                if (watch is null)
                {
                    context.Watches.Add(new Watch
                    {
                        WatchId = definition.WatchId,
                        PathToWatch = definition.PathToWatch,
                        Status = "active",
                        CallbackUrlTemplate = definition.CallbackUrlTemplate,
                        CallbackPayloadTemplate = definition.CallbackPayloadTemplate,
                        Version = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
                else
                {
                    UpdateWatch(watch, definition, "active", now);
                }

                return "applied";

            case "suspend_watcher":
                var watchToSuspend = EnsureWatchExists(watch, definition.WatchId);
                watchToSuspend.Status = "suspended";
                watchToSuspend.Version++;
                watchToSuspend.UpdatedAt = now;
                return "applied";

            case "resume_watcher":
                UpdateWatch(EnsureWatchExists(watch, definition.WatchId), definition, "active", now);
                return "applied";

            default:
                throw new InvalidOperationException($"Unsupported watcher control command '{commandType}'.");
        }
    }

    private static void UpdateWatch(Watch watch, WatchDefinition definition, string status, DateTimeOffset now)
    {
        watch.PathToWatch = definition.PathToWatch;
        watch.Status = status;
        watch.CallbackUrlTemplate = definition.CallbackUrlTemplate;
        watch.CallbackPayloadTemplate = definition.CallbackPayloadTemplate;
        watch.Version++;
        watch.UpdatedAt = now;
    }

    private static Watch EnsureWatchExists(Watch? watch, string watchId)
    {
        if (watch is null)
        {
            throw new InvalidOperationException($"Watch '{watchId}' does not exist.");
        }

        return watch;
    }
}
