# Ownership Lanes

Named lanes describe repository ownership boundaries. They are not separate
services unless implementation docs say so.

## Rules

- Use the narrowest lane that owns the files or behavior being changed.
- Give production-adjacent work an explicit path list, validation command, and stop condition.
- Do not let parallel work edit the same files at the same time.
- Escalate if a task crosses lanes without an architecture or plan update.

## Stop Conditions

Stop and report instead of improvising when work needs:

- Secrets, credentials, cloud billing, or external account access.
- Destructive Git operations.
- Schema or contract decisions that affect multiple phases.
- Changes outside the assigned path list.
- A failing validation command with an unclear cause.
- Product or architecture decisions that conflict with current docs.

## Lane Roster

### Atlas - Architecture

Owns system shape, ADRs, service boundaries, workflow boundaries, and non-functional requirements.

### Mount - Ingest Watcher

Owns filesystem mount watching, package detection, manifest detection, and done marker detection.

### Pulse - Workflow

Owns Dapr Workflow orchestration, child workflow structure, workflow events, and graph projection semantics.

### Courier - Messaging And Outbox

Owns Azure Service Bus routing, command/event contracts at the broker boundary, and outbox dispatch behavior.

### Vault - Persistence

Owns PostgreSQL persistence, business state, audit/timeline storage, and data access boundaries.

### Essence - Media Agents

Owns video, audio, text, sidecar, proxy, metadata, QC, and other specialized agents.

### Beacon - Observability

Owns structured logs, OpenTelemetry traces, correlation fields, metrics, and diagnostics.

### Canvas - Workflow UI

Owns the ingest control plane UI, workflow graph rendering, nested workflow drilldown, and node log views.

### Forge - Platform

Owns Docker, local runtime, Kubernetes, Dapr components, Azure deployment docs, and host tooling.

### Gauge - QA

Owns test strategy, integration tests, smoke tests, and validation evidence.

### Shield - Security

Owns secrets policy, identity boundaries, container safety, and cloud security review.
