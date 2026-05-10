using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MediaIngest.Worker.LocalFileSystemWatcher.Migrations;

[DbContext(typeof(WatcherDbContext))]
[Migration("20260510000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "local_file_system_watcher");

        migrationBuilder.CreateTable(
            name: "watches",
            schema: "local_file_system_watcher",
            columns: table => new
            {
                watch_id = table.Column<string>(type: "text", nullable: false),
                path_to_watch = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                callback_url_template = table.Column<string>(type: "text", nullable: false),
                callback_payload_template = table.Column<string>(type: "text", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_watches", watch => watch.watch_id);
            });

        migrationBuilder.CreateTable(
            name: "control_commands",
            schema: "local_file_system_watcher",
            columns: table => new
            {
                command_id = table.Column<string>(type: "text", nullable: false),
                watch_id = table.Column<string>(type: "text", nullable: false),
                command_type = table.Column<string>(type: "text", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                result = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_control_commands", command => command.command_id);
            });

        migrationBuilder.CreateTable(
            name: "events",
            schema: "local_file_system_watcher",
            columns: table => new
            {
                event_id = table.Column<string>(type: "text", nullable: false),
                watch_id = table.Column<string>(type: "text", nullable: false),
                event_type = table.Column<string>(type: "text", nullable: false),
                is_file = table.Column<bool>(type: "boolean", nullable: false),
                target_event_source_path = table.Column<string>(type: "text", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                callback_url = table.Column<string>(type: "text", nullable: false),
                callback_payload_json = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_events", watchEvent => watchEvent.event_id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            schema: "local_file_system_watcher",
            columns: table => new
            {
                message_id = table.Column<string>(type: "text", nullable: false),
                event_id = table.Column<string>(type: "text", nullable: false),
                destination = table.Column<string>(type: "text", nullable: false),
                message_type = table.Column<string>(type: "text", nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                dispatch_claim_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_outbox_messages", message => message.message_id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "control_commands", schema: "local_file_system_watcher");
        migrationBuilder.DropTable(name: "events", schema: "local_file_system_watcher");
        migrationBuilder.DropTable(name: "outbox_messages", schema: "local_file_system_watcher");
        migrationBuilder.DropTable(name: "watches", schema: "local_file_system_watcher");
    }
}
