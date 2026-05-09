# Parallel System Components Plan

This plan slices the next local system-component work into independent tracks
that can run in separate worktrees. The current demo contract stays unchanged:
`input/<package-id>/...` produces `output/<package-id>/...`.

## Coordination Rules

- Use `.terminals/` only for local, gitignored helper scripts.
- Keep durable state in repo docs.
- Keep live worktree coordination in ignored `.worktrees/state/<worktree-slug>.md`
  files.
- Each track owns only its listed target files.
- Stop when a task needs a shared file, schema or contract decision, dependency
  change, secret, cloud action, paid resource, or destructive Git operation.
- The coordinator serializes shared edits to `Makefile`, `package.json`,
  `MediaIngest.sln`, and shared docs.
- Do not change the existing local ingest contract until a later integration
  task explicitly does so.

## Tracks

| Track | Lane | Scope | Target files | Forbidden files | Validation |
| --- | --- | --- | --- | --- | --- |
| 01 Forge Docker Runtime | Forge | Add local compose for API, UI, and PostgreSQL with repo `input/` and `output/` bind mounts. | `deploy/docker`, compose files | Shared solution, app code, package scripts unless coordinator-owned | `docker compose config` from the added compose file |
| 02 Vault PostgreSQL Persistence | Vault | Add package-state and outbox schema plus `PostgresIngestPersistenceStore` matching `IIngestPersistenceStore`; keep API on in-memory store. | `src/MediaIngest.Persistence`, `tests/MediaIngest.Persistence.Tests` | API wiring, workflow, UI, shared command runners | `make test-dotnet-persistence` |
| 03 Canvas Local Status UI | Canvas | Keep mocked workflow graph and add a separate real package status panel backed by `GET /api/ingest/status`. | `web/ingest-control-plane` | Backend contracts unless coordinator-approved | UI test command for the package |
| 04 Mount File Discovery | Mount | Enumerate every physical file under ready package directories without wiring discovery into copy behavior. | `src/MediaIngest.Worker.Watcher`, `tests/MediaIngest.Worker.Watcher.Tests` | Persistence, workflow, outbox, UI | `make test-dotnet-watcher` |
| 05 Essence Checksum Validator | Essence | Add standalone raw SHA-256 hex validation for `manifest.json.checksum`. | Essence/checksum component files and focused tests | Watcher copy behavior, persistence, workflow | Focused checksum tests |
| 06 Courier Outbox Shape | Courier | Refine local outbox dispatch contracts/tests for pending messages and dispatched marking; no Azure Service Bus. | `src/MediaIngest.Worker.Outbox`, outbox tests | Azure Service Bus, Dapr wiring, persistence schema unless coordinated | `make test-dotnet-outbox` |
| 07 Pulse Workflow Boundary | Pulse | Add minimal in-process lifecycle boundary for observed, ready, started, succeeded, and failed; no Dapr runtime wiring. | `src/MediaIngest.Workflow`, `tests/MediaIngest.Workflow.Tests` | Dapr runtime config, watcher, persistence schema | `make test-dotnet-workflow` |
| 08 Beacon Runtime Diagnostics | Beacon | Add structured diagnostic event names for scan, readiness, copy, outbox dispatch, success, and failure. | `src/MediaIngest.Observability`, `tests/MediaIngest.Observability.Tests` | Functional workflow, watcher, outbox behavior | `make test-dotnet-observability` |
| 09 Gauge E2E Smoke | Gauge | Add a local smoke script that starts clean, posts start, creates package files, and asserts output files. | `scripts/dev`, smoke-test docs | Runtime compose files, app behavior, solution files | Local smoke dry-run or shell syntax plus documented manual command |
| 10 Coordinator Integration | Forge | Serially update shared files and integrate only validated branches. | `Makefile`, `package.json`, `MediaIngest.sln`, shared docs | Lane-owned implementation files unless integrating validated branches | `make validate` |

## Handoff Requirements

Each track must report:

- task ID or local track ID
- linked user stories
- ownership lane
- files changed
- validation command and outcome
- local `.worktrees/state/` updates
- blockers and any contract conflicts
- PR state when PR creation is authorized

## Test Plan

- Docker: compose config/build validation and local PostgreSQL startup.
- PostgreSQL: save batch, query pending outbox, mark dispatched, package status
  projection.
- UI: Vitest for Start ingest and real status rendering.
- Watcher: file discovery and readiness tests.
- Checksum: valid, missing, malformed, and mismatched checksum tests.
- Outbox: pending dispatch and idempotent dispatched marking tests.
- Workflow: lifecycle transition tests.
- Observability: expected diagnostic event names.
- Smoke: end-to-end `input/<package-id>` to `output/<package-id>` assertion.
