namespace MediaIngest.Persistence;

internal static class PostgresIngestSchema
{
    public const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS ingest_package_states (
            package_id text PRIMARY KEY,
            workflow_instance_id text NOT NULL,
            status text NOT NULL,
            updated_at timestamptz NOT NULL
        );

        CREATE TABLE IF NOT EXISTS outbox_messages (
            message_id text PRIMARY KEY,
            destination text NOT NULL,
            message_type text NOT NULL,
            payload_json jsonb NOT NULL,
            correlation_id text NOT NULL,
            created_at timestamptz NOT NULL,
            dispatched_at timestamptz NULL,
            dispatch_claim_expires_at timestamptz NULL
        );

        ALTER TABLE outbox_messages
            ADD COLUMN IF NOT EXISTS dispatch_claim_expires_at timestamptz NULL;

        CREATE INDEX IF NOT EXISTS idx_outbox_messages_pending
            ON outbox_messages (created_at, message_id)
            WHERE dispatched_at IS NULL;

        CREATE TABLE IF NOT EXISTS business_timeline_records (
            event_id text PRIMARY KEY,
            workflow_instance_id text NOT NULL,
            node_id text NOT NULL,
            package_id text NOT NULL,
            correlation_id text NOT NULL,
            occurred_at timestamptz NOT NULL,
            status text NOT NULL,
            message text NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_business_timeline_records_workflow_node
            ON business_timeline_records (workflow_instance_id, node_id, occurred_at, event_id);

        CREATE TABLE IF NOT EXISTS node_diagnostic_logs (
            log_id text PRIMARY KEY,
            workflow_instance_id text NOT NULL,
            node_id text NOT NULL,
            package_id text NOT NULL,
            correlation_id text NOT NULL,
            occurred_at timestamptz NOT NULL,
            level text NOT NULL,
            message text NOT NULL,
            trace_id text NULL,
            span_id text NULL
        );

        CREATE INDEX IF NOT EXISTS idx_node_diagnostic_logs_workflow_node
            ON node_diagnostic_logs (workflow_instance_id, node_id, occurred_at, log_id);
        """;
}
