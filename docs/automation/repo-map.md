# Repository Map

Planned structure:

- `src/MediaIngest.Api` - API for the ingest control plane UI.
- `src/MediaIngest.Worker.LocalFileSystemWatcher` - generic local filesystem
  watcher service with EF-owned desired state, events, and callback outbox.
- `src/MediaIngest.Worker.Watcher` - filesystem package watcher.
- `src/MediaIngest.Worker.Outbox` - transactional outbox dispatcher.
- `src/MediaIngest.Workflow` - Dapr workflow definitions and orchestration contracts.
- command runner services - future light, medium, and heavy generic command
  executors.
- `src/MediaIngest.Contracts` - command, routing, event, and DTO contracts.
- `src/MediaIngest.Persistence` - PostgreSQL persistence and outbox support.
- `src/MediaIngest.Observability` - logging, tracing, and correlation helpers.
- `web/ingest-control-plane` - workflow visualization UI.
- `deploy/docker` - container build and local runtime assets.
- `deploy/k8s` - Kubernetes manifests or Helm assets.
- `deploy/dapr` - Dapr components and configuration.
- `scripts/dev` - local developer bootstrap, checks, and lightweight validation helpers.
- `docs/automation` - compact agent execution context.
- `docs/architecture`, `docs/adr`, `docs/product`, `docs/standards` - durable project docs.
- `docs/plans` - milestone and task planning artifacts.
- `docs/bugs` - defect reports and bug-fix tracking.
- `docs/status` - current project status, decisions, and work log.
- `Makefile` - canonical local command entrypoint.
- `package.json` - repo-level npm scripts for documentation and tooling checks.

The current repository is still in planning/foundation state. Do not assume the
planned source tree exists until implementation begins.
