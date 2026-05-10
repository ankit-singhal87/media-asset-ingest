using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MediaIngest.Worker.LocalFileSystemWatcher.Migrations;

[DbContext(typeof(WatcherDbContext))]
public sealed class WatcherDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("local_file_system_watcher");

        modelBuilder.Entity<Watch>(builder =>
        {
            builder.ToTable("watches", "local_file_system_watcher");
            builder.HasKey(watch => watch.WatchId);
            builder.Property(watch => watch.WatchId).HasColumnName("watch_id");
            builder.Property(watch => watch.PathToWatch).HasColumnName("path_to_watch");
            builder.Property(watch => watch.Status).HasColumnName("status");
            builder.Property(watch => watch.CallbackUrlTemplate).HasColumnName("callback_url_template");
            builder.Property(watch => watch.CallbackPayloadTemplate).HasColumnName("callback_payload_template");
            builder.Property(watch => watch.Version).HasColumnName("version");
            builder.Property(watch => watch.CreatedAt).HasColumnName("created_at");
            builder.Property(watch => watch.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ControlCommand>(builder =>
        {
            builder.ToTable("control_commands", "local_file_system_watcher");
            builder.HasKey(command => command.CommandId);
            builder.Property(command => command.CommandId).HasColumnName("command_id");
            builder.Property(command => command.WatchId).HasColumnName("watch_id");
            builder.Property(command => command.CommandType).HasColumnName("command_type");
            builder.Property(command => command.ReceivedAt).HasColumnName("received_at");
            builder.Property(command => command.AppliedAt).HasColumnName("applied_at");
            builder.Property(command => command.Result).HasColumnName("result");
        });

        modelBuilder.Entity<WatchEvent>(builder =>
        {
            builder.ToTable("events", "local_file_system_watcher");
            builder.HasKey(watchEvent => watchEvent.EventId);
            builder.Property(watchEvent => watchEvent.EventId).HasColumnName("event_id");
            builder.Property(watchEvent => watchEvent.WatchId).HasColumnName("watch_id");
            builder.Property(watchEvent => watchEvent.EventType).HasColumnName("event_type");
            builder.Property(watchEvent => watchEvent.IsFile).HasColumnName("is_file");
            builder.Property(watchEvent => watchEvent.TargetEventSourcePath).HasColumnName("target_event_source_path");
            builder.Property(watchEvent => watchEvent.OccurredAt).HasColumnName("occurred_at");
            builder.Property(watchEvent => watchEvent.CallbackUrl).HasColumnName("callback_url");
            builder.Property(watchEvent => watchEvent.CallbackPayloadJson).HasColumnName("callback_payload_json");
            builder.Property(watchEvent => watchEvent.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<CallbackOutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages", "local_file_system_watcher");
            builder.HasKey(message => message.MessageId);
            builder.Property(message => message.MessageId).HasColumnName("message_id");
            builder.Property(message => message.EventId).HasColumnName("event_id");
            builder.Property(message => message.Destination).HasColumnName("destination");
            builder.Property(message => message.MessageType).HasColumnName("message_type");
            builder.Property(message => message.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
            builder.Property(message => message.CreatedAt).HasColumnName("created_at");
            builder.Property(message => message.DispatchedAt).HasColumnName("dispatched_at");
            builder.Property(message => message.DispatchClaimExpiresAt).HasColumnName("dispatch_claim_expires_at");
        });
    }
}
