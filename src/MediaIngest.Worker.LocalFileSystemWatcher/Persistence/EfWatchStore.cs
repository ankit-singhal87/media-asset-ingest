using Microsoft.EntityFrameworkCore;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class EfWatchStore(WatcherDbContext context) : IWatchStore
{
    public async Task<ControlCommand?> FindControlCommandAsync(
        string commandId,
        CancellationToken cancellationToken = default)
    {
        return await context.ControlCommands.FindAsync([commandId], cancellationToken);
    }

    public async Task<Watch?> FindWatchAsync(
        string watchId,
        CancellationToken cancellationToken = default)
    {
        return await context.Watches.SingleOrDefaultAsync(
            candidate => candidate.WatchId == watchId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Watch>> ListDesiredWatchesAsync(CancellationToken cancellationToken = default)
    {
        return await context.Watches
            .AsNoTracking()
            .OrderBy(watch => watch.WatchId)
            .ToListAsync(cancellationToken);
    }

    public void AddWatch(Watch watch)
    {
        context.Watches.Add(watch);
    }

    public void AddControlCommand(ControlCommand command)
    {
        context.ControlCommands.Add(command);
    }

    public async Task SaveWatchEventWithOutboxAsync(
        WatchEvent watchEvent,
        CallbackOutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        context.Events.Add(watchEvent);
        context.OutboxMessages.Add(outboxMessage);
        await context.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(
        CancellationToken cancellationToken)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return null;
        }

        return await context.Database.BeginTransactionAsync(cancellationToken);
    }
}
