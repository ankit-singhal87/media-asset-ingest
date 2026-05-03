namespace MediaIngest.Persistence;

public static class PostgresIngestSchema
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
            dispatched_at timestamptz NULL
        );

        CREATE INDEX IF NOT EXISTS idx_outbox_messages_pending
            ON outbox_messages (created_at, message_id)
            WHERE dispatched_at IS NULL;
        """;
}
