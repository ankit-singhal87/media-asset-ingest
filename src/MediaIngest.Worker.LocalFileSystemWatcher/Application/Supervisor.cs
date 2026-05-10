namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal interface IWatcherRuntime
{
    Task StartAsync(WatchDefinition definition, CancellationToken cancellationToken = default);

    Task StopAsync(string watchId, CancellationToken cancellationToken = default);
}

internal interface IReconciliationSignal
{
    void RequestReconciliation();
}

internal sealed class Supervisor(IWatchStore store, IWatcherRuntime runtime) : IReconciliationSignal
{
    private readonly Dictionary<string, LocalWatchState> localWatches = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim reconciliationRequested = new(0, int.MaxValue);

    public void RequestReconciliation()
    {
        reconciliationRequested.Release();
    }

    public async Task RunAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pollingInterval, TimeSpan.Zero);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await reconciliationRequested.WaitAsync(pollingInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var desiredWatches = await store.ListDesiredWatchesAsync(cancellationToken);

        foreach (var watch in desiredWatches)
        {
            if (watch.Status == "active")
            {
                await ReconcileActiveWatchAsync(watch, cancellationToken);
                continue;
            }

            if (watch.Status == "suspended")
            {
                await StopLocalWatchIfPresentAsync(watch.WatchId, cancellationToken);
            }
        }

        foreach (var staleWatchId in localWatches.Keys.Except(desiredWatches.Select(watch => watch.WatchId)).ToArray())
        {
            await StopLocalWatchIfPresentAsync(staleWatchId, cancellationToken);
        }
    }

    private async Task ReconcileActiveWatchAsync(Watch watch, CancellationToken cancellationToken)
    {
        if (localWatches.TryGetValue(watch.WatchId, out var localWatch) && localWatch.Version == watch.Version)
        {
            return;
        }

        if (localWatch is not null)
        {
            await runtime.StopAsync(watch.WatchId, cancellationToken);
        }

        var definition = new WatchDefinition(
            watch.WatchId,
            watch.PathToWatch,
            watch.CallbackUrlTemplate,
            watch.CallbackPayloadTemplate);
        await runtime.StartAsync(definition, cancellationToken);
        localWatches[watch.WatchId] = new LocalWatchState(watch.Version);
    }

    private async Task StopLocalWatchIfPresentAsync(string watchId, CancellationToken cancellationToken)
    {
        if (!localWatches.Remove(watchId))
        {
            return;
        }

        await runtime.StopAsync(watchId, cancellationToken);
    }

    private sealed record LocalWatchState(int Version);
}
