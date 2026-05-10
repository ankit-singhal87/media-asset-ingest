using Microsoft.EntityFrameworkCore;

namespace MediaIngest.Worker.LocalFileSystemWatcher;

internal sealed class WatcherDbContext(DbContextOptions<WatcherDbContext> options) : DbContext(options)
{
    public DbSet<Watch> Watches => Set<Watch>();

    public DbSet<WatchEvent> Events => Set<WatchEvent>();

    public DbSet<ControlCommand> ControlCommands => Set<ControlCommand>();

    public DbSet<CallbackOutboxMessage> OutboxMessages => Set<CallbackOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("local_file_system_watcher");

        modelBuilder.Entity<Watch>(builder =>
        {
            builder.ToTable("watches");
            builder.HasKey(watch => watch.WatchId);
        });

        modelBuilder.Entity<ControlCommand>(builder =>
        {
            builder.ToTable("control_commands");
            builder.HasKey(command => command.CommandId);
        });

        modelBuilder.Entity<WatchEvent>(builder =>
        {
            builder.ToTable("events");
            builder.HasKey(watchEvent => watchEvent.EventId);
        });

        modelBuilder.Entity<CallbackOutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(message => message.MessageId);
            builder.Property(message => message.PayloadJson).HasColumnType("jsonb");
        });
    }
}
