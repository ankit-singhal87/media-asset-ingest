using System.Collections.Concurrent;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class FileSystemWatcherRuntime(EventRecorder eventRecorder) : IWatcherRuntime, IDisposable
{
    private readonly ConcurrentDictionary<string, FileSystemWatcher> watchers = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> knownPathTypes = new(StringComparer.Ordinal);
    private readonly List<Task> pendingRecords = [];
    private readonly SemaphoreSlim recordLock = new(1, 1);
    private readonly object pendingRecordsLock = new();

    public Task StartAsync(WatchDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(definition.PathToWatch))
        {
            throw new DirectoryNotFoundException($"Watch path '{definition.PathToWatch}' does not exist.");
        }

        SeedKnownPathTypes(definition.PathToWatch);

        var watcher = new FileSystemWatcher(definition.PathToWatch)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        watcher.Created += (_, args) => Record(definition.WatchId, "created", args.FullPath, ClassifyPath(args.FullPath));
        watcher.Changed += (_, args) => Record(definition.WatchId, "changed", args.FullPath, ClassifyPath(args.FullPath));
        watcher.Deleted += (_, args) => Record(definition.WatchId, "deleted", args.FullPath, ClassifyDeletedPath(args.FullPath));
        watcher.Renamed += (object _, RenamedEventArgs args) =>
        {
            knownPathTypes.TryRemove(args.OldFullPath, out var _);
            Record(definition.WatchId, "renamed", args.FullPath, ClassifyPath(args.FullPath));
        };

        if (watchers.TryRemove(definition.WatchId, out var existing))
        {
            existing.Dispose();
        }

        watchers[definition.WatchId] = watcher;
        return Task.CompletedTask;
    }

    public async Task StopAsync(string watchId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(watchId);
        cancellationToken.ThrowIfCancellationRequested();

        if (watchers.TryRemove(watchId, out var watcher))
        {
            watcher.Dispose();
        }

        await DrainPendingRecordsAsync();
    }

    public void Dispose()
    {
        foreach (var watcher in watchers.Values)
        {
            watcher.Dispose();
        }

        watchers.Clear();
    }

    private void Record(string watchId, string eventType, string path, bool isFile)
    {
        var observedEvent = new ObservedFileSystemEvent(
            watchId,
            eventType,
            isFile,
            path,
            DateTimeOffset.UtcNow);

        lock (pendingRecordsLock)
        {
            var recordTask = Task.Run(async () =>
            {
                await recordLock.WaitAsync();
                try
                {
                    await eventRecorder.RecordAsync(observedEvent);
                }
                finally
                {
                    recordLock.Release();
                }
            });
            pendingRecords.Add(recordTask);
        }
    }

    private void SeedKnownPathTypes(string watchPath)
    {
        foreach (var directory in Directory.EnumerateDirectories(watchPath, "*", SearchOption.AllDirectories))
        {
            knownPathTypes[directory] = false;
        }

        foreach (var file in Directory.EnumerateFiles(watchPath, "*", SearchOption.AllDirectories))
        {
            knownPathTypes[file] = true;
        }
    }

    private bool ClassifyPath(string path)
    {
        var isFile = File.Exists(path) || !Directory.Exists(path);
        knownPathTypes[path] = isFile;
        return isFile;
    }

    private bool ClassifyDeletedPath(string path)
    {
        if (knownPathTypes.TryRemove(path, out var isFile))
        {
            return isFile;
        }

        return File.Exists(path);
    }

    private async Task DrainPendingRecordsAsync()
    {
        while (true)
        {
            Task[] recordsToDrain;
            lock (pendingRecordsLock)
            {
                recordsToDrain = pendingRecords.ToArray();
                pendingRecords.Clear();
            }

            if (recordsToDrain.Length == 0)
            {
                return;
            }

            await Task.WhenAll(recordsToDrain);
        }
    }
}
