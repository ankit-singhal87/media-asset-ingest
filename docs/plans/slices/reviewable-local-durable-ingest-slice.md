# Reviewable Local Durable Ingest Slice

## Intent

Make the current local media ingest implementation externally reviewable as a
small product slice: API, UI, PostgreSQL state, outbox evidence, workflow graph,
node diagnostics, and local Docker Compose startup should all run without Azure
resources, secrets, or paid cloud actions.

## Scope

- Docker Compose runs API, UI, PostgreSQL, an outbox worker host, and
  light/medium/heavy command-runner host processes.
- `MediaIngest.Api` selects persistence by configuration:
  - `Persistence:Provider=InMemory` or unset keeps focused tests lightweight.
  - `Persistence:Provider=Postgres` uses raw Npgsql and requires an explicit
    connection string.
- Local Compose sets `Persistence:CreateSchemaOnStartup=true` so the API can
  create the current review schema before serving requests.
- API missing workflow and node-detail responses use Problem Details with the
  ASP.NET trace identifier.
- The local runtime smoke asserts HTTP behavior plus durable PostgreSQL package
  state and dispatched command outbox rows.

## Non-Goals

- No Azure Service Bus SDK integration.
- No real Dapr Workflow SDK hosting.
- No cloud resources, Terraform apply, production deployment, load balancers,
  secrets, or paid cloud actions.
- No Entity Framework Core migration workflow in this slice; PostgreSQL access
  remains raw Npgsql behind `IIngestPersistenceStore`.

## Review Evidence

- `make local-compose-check` validates the Compose graph.
- `make local-runtime-smoke` starts the local runtime, posts an ingest package,
  verifies API/UI HTTP readiness, checks workflow command-node evidence, and
  queries PostgreSQL for durable state/outbox evidence.
- `make test-dotnet-api`, `make test-dotnet-persistence`,
  `make test-dotnet-outbox`, and `make test-dotnet-command-runner` cover the
  focused backend boundaries.
- `docs/api/openapi.yaml` documents the current HTTP contract.
