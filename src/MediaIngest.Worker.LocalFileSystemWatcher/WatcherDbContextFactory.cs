using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class WatcherDbContextFactory : IDesignTimeDbContextFactory<WatcherDbContext>
{
    public WatcherDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LOCAL_FILE_SYSTEM_WATCHER_POSTGRES")
            ?? "Host=localhost;Database=media_ingest;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<WatcherDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new WatcherDbContext(options);
    }
}
