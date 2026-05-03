# Milestones

Milestones are numbered delivery slices. Each milestone maps to user stories,
primary domains, ownership lanes, and planned components.

## MILESTONE-1: Documentation And Local Foundation

Status: Complete

Purpose: Establish the project operating model, architecture decisions, product
backlog, standards, and Docker-first developer workflow.

User stories:

- USER-STORY-16
- USER-STORY-17

Domains:

- Project operations
- Developer experience

Ownership lanes:

- Atlas
- Forge
- Gauge

Components:

- `AGENTS.md`
- `docs/automation`
- `docs/architecture`
- `docs/adr`
- `docs/product`
- `docs/standards`
- `docs/status`
- `Makefile`
- `scripts/dev`
- `package.json`

## MILESTONE-2: .NET Solution And Local Runtime

Status: In Progress

Purpose: Create the .NET solution, local container runtime, and baseline
developer commands.

User stories:

- USER-STORY-16
- USER-STORY-17

Domains:

- Platform
- Developer experience

Ownership lanes:

- Forge
- Gauge

Components:

- `src`
- `deploy/docker`
- `docker-compose.yml`
- `Makefile`
- `scripts/dev`

## MILESTONE-3: Ingest Package Lifecycle

Status: In Progress

Purpose: Detect ingest packages, gate work on manifest presence, enumerate real
files, classify essences, and reconcile on done marker.

User stories:

- USER-STORY-1
- USER-STORY-2
- USER-STORY-3
- USER-STORY-4
- USER-STORY-5

Domains:

- Ingest package
- Manifest
- Done marker
- Essence classification

Ownership lanes:

- Mount
- Essence
- Gauge

Components:

- `src/MediaIngest.Worker.Watcher`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Tests`

## MILESTONE-4: Messaging And Outbox

Status: In Progress

Purpose: Add PostgreSQL transactional outbox and Azure Service Bus command
routing for semantic command topics and capacity-based runners.

User stories:

- USER-STORY-6
- USER-STORY-8

Domains:

- Messaging
- Outbox
- Work item

Ownership lanes:

- Courier
- Vault
- Gauge

Components:

- `src/MediaIngest.Worker.Outbox`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `deploy/dapr`
- `deploy/docker`

## MILESTONE-5: Dapr Workflow Orchestration

Status: In Progress

Purpose: Add durable package orchestration with Dapr Workflow, child workflow
boundaries, signals, and workflow state separation.

User stories:

- USER-STORY-9
- USER-STORY-10

Domains:

- Workflow instance
- Workflow node
- Package lifecycle

Ownership lanes:

- Pulse
- Vault
- Gauge

Components:

- `src/MediaIngest.Workflow`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `deploy/dapr`

## MILESTONE-6: Command Runners

Status: In Progress

Purpose: Add independently scalable command runners for light, medium, and
heavy media work.

User stories:

- USER-STORY-7
- USER-STORY-11

Domains:

- Command execution
- Work item
- Runner execution

Ownership lanes:

- Essence
- Beacon
- Gauge

Components:

- `src/MediaIngest.Contracts`
- `src/MediaIngest.Observability`

## MILESTONE-7: Observability

Status: In Progress

Purpose: Add structured logs, OpenTelemetry traces, correlation fields, and
business timeline events.

User stories:

- USER-STORY-11

Domains:

- Observability
- Timeline
- Diagnostics

Ownership lanes:

- Beacon
- Vault
- Gauge

Components:

- `src/MediaIngest.Observability`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Contracts`

## MILESTONE-8: Workflow Visualization UI

Status: In Progress

Purpose: Add workflow graph APIs and UI for node status, nested workflow
drilldown, back traversal, and node log/timeline inspection.

User stories:

- USER-STORY-12
- USER-STORY-13
- USER-STORY-14
- USER-STORY-15

Domains:

- Workflow graph
- Workflow node
- Package timeline
- Operator control plane

Ownership lanes:

- Canvas
- Pulse
- Beacon
- Gauge

Components:

- `src/MediaIngest.Api`
- `web/ingest-control-plane`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Observability`

## MILESTONE-9: Kubernetes And Azure Readiness

Status: Planned

Purpose: Add Kubernetes manifests, Dapr components, and Azure deployment
documentation without requiring paid cloud execution by default.

User stories:

- USER-STORY-17

Domains:

- Platform
- Azure deployment
- Kubernetes runtime

Ownership lanes:

- Forge
- Shield
- Beacon

Components:

- `deploy/k8s`
- `deploy/dapr`
- `deploy/azure`
- `docs/architecture`
- `docs/automation`
