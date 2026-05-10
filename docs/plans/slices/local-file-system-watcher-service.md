# Local File System Watcher Service

## Intent

Plan a self-contained local filesystem watcher service that can be used by
ingest and other media asset workflows. The service observes configured local
directory paths, records durable filesystem events, and queues callback delivery
without embedding ingest-specific behavior.

## Boundary

- Service: `LocalFileSystemWatcher`
- Project: `MediaIngest.Worker.LocalFileSystemWatcher`
- PostgreSQL schema: `local_file_system_watcher`
- Primary runtime role: watch local filesystem directories only.

The service is intentionally local-filesystem specific. Azure Blob, S3, network
share, or other storage watchers should be separate services or later adapter
work, not implied by this first service.

## Naming

Avoid smurf naming inside the service namespace. Use the service and database
schema to carry the boundary name, then keep internal type and table names short.

Tables:

- `local_file_system_watcher.watches`
- `local_file_system_watcher.events`
- `local_file_system_watcher.control_commands`
- `local_file_system_watcher.outbox_messages`
- `local_file_system_watcher.leases` when horizontal scaling is introduced

Representative code types inside the service namespace:

- `Supervisor`
- `Watch`
- `WatchDefinition`
- `WatchEvent`
- `ControlCommandHandler`
- `CallbackTemplateRenderer`
- `OutboxDispatcher`
- `WatcherDbContext`

## Persistence

Use Entity Framework Core with the Npgsql provider. Do not use raw Npgsql for
this service's data access. Configure EF Core to own all objects under the
dedicated schema:

```csharp
modelBuilder.HasDefaultSchema("local_file_system_watcher");
```

Use EF Core migrations for schema creation and evolution. Entity configuration
should explicitly map table names to snake_case PostgreSQL identifiers:

```csharp
builder.ToTable("watches");
builder.ToTable("events");
builder.ToTable("control_commands");
builder.ToTable("outbox_messages");
```

The implementation session must account for the required EF Core/Npgsql
dependency change before editing project files.

## Data Model

`watches` stores durable desired state:

- `watch_id`
- `path_to_watch`
- `status`: `active` or `suspended`
- `callback_url_template`
- `callback_payload_template`
- `version`
- `created_at`
- `updated_at`

`control_commands` records idempotent command handling:

- `command_id`
- `watch_id`
- `command_type`: `create_watcher`, `suspend_watcher`, or `resume_watcher`
- `received_at`
- `applied_at`
- `result`

`events` records observed filesystem events:

- `event_id`
- `watch_id`
- `event_type`
- `is_file`
- `target_event_source_path`
- `occurred_at`
- `callback_url`
- `callback_payload_json`
- `created_at`

`outbox_messages` stores durable outbound callback work for retryable delivery.
Filesystem event handling should persist the event and enqueue the callback in
one database transaction.

`leases` should be added before running multiple active replicas. The initial
plan assumes one active watcher replica.

## Control Flow

Support these commands:

- `create_watcher`
- `suspend_watcher`
- `resume_watcher`

Commands may arrive through Dapr pub/sub on a watcher control topic. The
control handler validates each command, records idempotency in
`control_commands`, updates `watches`, and signals the supervisor to reconcile.

PostgreSQL remains the source of truth. Messages should wake reconciliation or
carry command intent; they must not be the only durable record of desired
watcher state.

## Supervisor Behavior

`Supervisor` keeps local runtime watchers in memory and reconciles them against
the `watches` table.

- `active` row with no local watcher starts a watcher.
- `suspended` row with a local watcher stops that watcher.
- Changed `version` recreates the local watcher when needed.
- Periodic polling handles missed messages and service restart recovery.
- Dapr control messages trigger immediate reconciliation.

The supervisor must not treat in-memory watchers as authoritative.

## Event And Callback Behavior

Filesystem events render callback templates using only the allowed tokens:

- `{eventType}`
- `{isFile}`
- `{targetEventSourcePath}`
- `{timestamp}`

Event types include at least:

- `created`
- `changed`
- `deleted`
- `renamed`

`timestamp` should be UTC with the best precision available from the runtime and
host operating system. The implementation should avoid promising true
nanosecond accuracy unless the runtime can prove it.

Callback delivery should be queued through `outbox_messages`, not performed
inline from the filesystem watcher callback.

## Non-Goals

- No ingest-specific package, manifest, or workflow logic inside this service.
- No Azure Blob, S3, or other storage provider watching in this slice.
- No horizontal multi-replica ownership until `leases` are designed and tested.
- No paid cloud actions, secret handling, or production deployment.

## Implementation Notes

- Prefer an HTTP or command API only as the external control-plane boundary.
  Do not expose RPC directly to the in-memory supervisor as the authoritative
  control path.
- If an API is added, it should persist desired state or enqueue commands
  durably, then let the supervisor reconcile.
- Use snake_case PostgreSQL identifiers and fully schema-owned EF mappings.
- Keep callback template validation strict so unsupported placeholders fail at
  command handling time instead of during event delivery.

## Implementation Status

Implemented in `MediaIngest.Worker.LocalFileSystemWatcher` as a local service
boundary with EF Core/Npgsql schema ownership, initial EF migration files,
durable watch desired state, idempotent control command records, filesystem
event records, callback outbox rows, strict callback template token handling,
and supervisor reconciliation against persisted watch state.

The implementation remains local-runtime scoped. It does not add Dapr pub/sub
hosting, HTTP control APIs, Azure storage watching, horizontal lease ownership,
or ingest-specific package behavior.
