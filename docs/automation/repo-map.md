# Repository Map

Planned structure:

- `src/MediaIngest.Api` - API for the ingest control plane UI.
- `src/MediaIngest.Worker.Watcher` - filesystem package watcher.
- `src/MediaIngest.Worker.Outbox` - transactional outbox dispatcher.
- `src/MediaIngest.Workflow` - Dapr workflow definitions and orchestration contracts.
- `src/MediaIngest.Agents.Video` - video/source essence agent.
- `src/MediaIngest.Agents.Audio` - audio essence agent.
- `src/MediaIngest.Agents.Text` - text essence agent.
- `src/MediaIngest.Agents.Other` - fallback and sidecar agent.
- `src/MediaIngest.Contracts` - command, event, and DTO contracts.
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
