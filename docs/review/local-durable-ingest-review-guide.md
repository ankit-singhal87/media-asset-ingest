# Local Durable Ingest Review Guide

## Current Status

This repository now presents a local, Docker-first ingest slice with durable
PostgreSQL business state, transactional outbox rows, a runnable API, a React
workflow UI, an outbox worker host, and light/medium/heavy command-runner host
processes. The slice is intentionally local and cloud-free.

## 10-Minute Review Path

1. Run `make local-compose-check`.
2. Run `make local-runtime-smoke`.
3. Open the UI at `http://127.0.0.1:5173`.
4. Inspect the API at `http://127.0.0.1:5000/api/ingest/status`.
5. Read the API contract in `docs/api/openapi.yaml`.

## What Works Today

- Compose starts API, UI, PostgreSQL, outbox worker, and command-runner worker
  containers.
- The API uses raw Npgsql-backed PostgreSQL persistence when Compose sets
  `Persistence__Provider=Postgres`.
- The API can create the local schema on startup when
  `Persistence__CreateSchemaOnStartup=true`.
- The local smoke posts an ingest start, creates a manifest-ready package,
  verifies copied manifest outputs, reads workflow graph command nodes, and
  queries PostgreSQL for package and outbox evidence.
- Missing workflow graph and node detail requests return Problem Details with a
  trace identifier.

## Known Gaps

- The command-runner hosts are runnable local review processes; real broker
  consumption remains represented by the command-runner library tests.
- The outbox worker validates command-bus message shape locally and marks rows
  dispatched; it does not publish to Azure Service Bus.
- Dapr Workflow SDK hosting and activity registration remain deferred.
- PostgreSQL schema management is guarded startup DDL for local Compose, not an
  EF Core or migration workflow.
- Node diagnostics are local persisted records, not production log aggregation
  or OpenTelemetry trace linkage.

## Source Anchors

- API runtime: `src/MediaIngest.Api/IngestApiApplication.cs`
- Runtime service: `src/MediaIngest.Api/IngestRuntimeService.cs`
- PostgreSQL store: `src/MediaIngest.Persistence/PostgresIngestPersistenceStore.cs`
- Schema DDL: `src/MediaIngest.Persistence/PostgresIngestSchema.cs`
- Outbox worker host: `src/MediaIngest.Worker.Outbox.Host/Program.cs`
- Command-runner host: `src/MediaIngest.Worker.CommandRunner.Host/Program.cs`
- Compose runtime: `deploy/docker/compose.yaml`
- Runtime smoke: `scripts/dev/local-compose-check.sh`
- API contract: `docs/api/openapi.yaml`
