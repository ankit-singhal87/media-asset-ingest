# User Stories

User stories are numbered and mapped to milestones, domains, components, and
ownership lanes. Keep detailed implementation tasks in `docs/plans/tasks`.

## Traceability Summary

| ID | Title | Milestone | Domain | Primary lane | Status |
| --- | --- | --- | --- | --- | --- |
| USER-STORY-1 | Watch ingest mount | MILESTONE-3 | Ingest package | Mount | In Progress |
| USER-STORY-2 | Start only when manifest exists | MILESTONE-3 | Manifest | Mount | Planned |
| USER-STORY-3 | Ingest all discovered files | MILESTONE-3 | Ingest package | Mount | Planned |
| USER-STORY-4 | Reconcile on done marker | MILESTONE-3 | Done marker | Mount | Planned |
| USER-STORY-5 | Classify media essences | MILESTONE-3 | Essence classification | Essence | Planned |
| USER-STORY-6 | Route work through ASB queues | MILESTONE-4 | Messaging | Courier | Planned |
| USER-STORY-7 | Process with specialized agents | MILESTONE-6 | Agent execution | Essence | In Progress |
| USER-STORY-8 | Use transactional outbox | MILESTONE-4 | Outbox | Courier | In Progress |
| USER-STORY-9 | Orchestrate package lifecycle with Dapr | MILESTONE-5 | Workflow | Pulse | In Progress |
| USER-STORY-10 | Support nested workflows | MILESTONE-5 | Workflow | Pulse | Planned |
| USER-STORY-11 | Record agent progress | MILESTONE-6, MILESTONE-7 | Observability | Beacon | In Progress |
| USER-STORY-12 | Visualize workflow execution | MILESTONE-8 | Workflow UI | Canvas | In Progress |
| USER-STORY-13 | Inspect node logs | MILESTONE-8 | Workflow UI | Canvas | Planned |
| USER-STORY-14 | Drill into nested workflows | MILESTONE-8 | Workflow UI | Canvas | Planned |
| USER-STORY-15 | Navigate back from child workflows | MILESTONE-8 | Workflow UI | Canvas | Planned |
| USER-STORY-16 | Develop with Docker-first tooling | MILESTONE-1, MILESTONE-2 | Developer experience | Forge | In Progress |
| USER-STORY-17 | Deploy to Kubernetes | MILESTONE-2, MILESTONE-9 | Platform | Forge | Planned |

## USER-STORY-1: Watch Ingest Mount

As an ingest operator, I want the system to monitor a configured mounted
directory, so that new media packages are detected automatically.

Milestone: MILESTONE-3

Domains:

- Ingest package
- Filesystem mount

Ownership lanes:

- Mount
- Gauge

Components involved:

- `src/MediaIngest.Worker.Watcher`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Tests`

Acceptance themes:

- Watch path is configuration-driven.
- Local development can use local filesystem storage.
- Production storage backing is hidden behind the mount.
- Package discovery emits durable business state or commands.

Dependencies:

- MILESTONE-2 local runtime foundation.

## USER-STORY-2: Start Only When Manifest Exists

As an ingest producer, I want ingest to start only after `manifest.json` exists,
so that incomplete package directories are not processed prematurely.

Milestone: MILESTONE-3

Domains:

- Manifest
- Ingest package

Ownership lanes:

- Mount
- Gauge

Components involved:

- `src/MediaIngest.Worker.Watcher`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Tests`

Acceptance themes:

- Package directory without `manifest.json` is observed but not processed.
- Package work starts after `manifest.json` appears.
- Manifest is treated as metadata and a start signal.

Dependencies:

- USER-STORY-1

## USER-STORY-3: Ingest All Discovered Files

As a MAM operator, I want the system to ingest every file physically present in
the package directory, so that incorrect manifests do not cause files to be
skipped.

Milestone: MILESTONE-3

Domains:

- Ingest package
- Discovered file
- Manifest validation

Ownership lanes:

- Mount
- Essence
- Gauge

Components involved:

- `src/MediaIngest.Worker.Watcher`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Tests`

Acceptance themes:

- Directory scan is source of truth for file presence.
- Files omitted from manifest are still ingested.
- Manifest references to missing files create warnings, not skipped real files.

Dependencies:

- USER-STORY-2

## USER-STORY-4: Reconcile On Done Marker

As an ingest operator, I want the system to rescan a package when the done marker
appears, so that late-arriving files are not missed.

Milestone: MILESTONE-3

Domains:

- Done marker
- Reconciliation
- Ingest package

Ownership lanes:

- Mount
- Pulse
- Gauge

Components involved:

- `src/MediaIngest.Worker.Watcher`
- `src/MediaIngest.Workflow`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`

Acceptance themes:

- Work may begin before done marker exists.
- Done marker triggers final package rescan.
- Late files are enqueued before package finalization.

Dependencies:

- USER-STORY-3
- USER-STORY-9

## USER-STORY-5: Classify Media Essences

As an ingest system, I want to classify discovered files by essence type, so
that video, audio, text, and sidecar files can be routed to specialized agents.

Milestone: MILESTONE-3

Domains:

- Essence classification
- Discovered file
- Work item

Ownership lanes:

- Essence
- Courier
- Gauge

Components involved:

- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Agents.Other`
- `src/MediaIngest.Tests`

Acceptance themes:

- `.mov`, `.mxf`, and `.mp4` classify as video/source.
- `.srt`, `.txt`, and `.vtt` classify as text.
- `.mp3` and `.wav` classify as audio.
- Unknown files classify as other.

Dependencies:

- USER-STORY-3

## USER-STORY-6: Route Work Through ASB Queues

As a platform engineer, I want file processing commands routed to named Azure
Service Bus queues, so that each specialized agent only consumes work it owns.

Milestone: MILESTONE-4

Domains:

- Messaging
- Work item

Ownership lanes:

- Courier
- Essence
- Gauge

Components involved:

- `src/MediaIngest.Contracts`
- `src/MediaIngest.Worker.Outbox`
- `deploy/dapr`
- `deploy/docker`

Acceptance themes:

- Each command has a specific destination queue.
- Agents consume only their assigned queues.
- Queue names are documented and stable.

Dependencies:

- USER-STORY-5
- USER-STORY-8

## USER-STORY-7: Process With Specialized Agents

As an operator, I want specialized agents for video, audio, text, proxy, and
other processing, so that each processing concern can scale and fail
independently.

Milestone: MILESTONE-6

Domains:

- Agent execution
- Essence processing
- Work item

Ownership lanes:

- Essence
- Beacon
- Gauge

Components involved:

- `src/MediaIngest.Agents.Video`
- `src/MediaIngest.Agents.Audio`
- `src/MediaIngest.Agents.Text`
- `src/MediaIngest.Agents.Other`
- `src/MediaIngest.Contracts`

Acceptance themes:

- Agent handlers are idempotent.
- Agent failures are recorded in business state.
- Agents can emit downstream work when allowed by workflow design.

Dependencies:

- USER-STORY-6
- USER-STORY-9

## USER-STORY-8: Use Transactional Outbox

As a backend engineer, I want state changes and outbound messages committed
atomically, so that ingest work is not lost when services crash.

Milestone: MILESTONE-4

Domains:

- Outbox
- Persistence
- Messaging

Ownership lanes:

- Courier
- Vault
- Gauge

Components involved:

- `src/MediaIngest.Persistence`
- `src/MediaIngest.Worker.Outbox`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Tests`

Acceptance themes:

- Business state and outbox messages commit in one transaction.
- Outbox dispatcher publishes decided messages.
- Dispatcher does not make domain routing decisions.
- Repeated dispatcher runs are safe.

Dependencies:

- MILESTONE-2 local runtime foundation.

## USER-STORY-9: Orchestrate Package Lifecycle With Dapr

As an ingest operator, I want a durable workflow per ingest package, so that
package progress survives crashes and can coordinate asynchronous agents.

Milestone: MILESTONE-5

Domains:

- Workflow instance
- Package lifecycle

Ownership lanes:

- Pulse
- Courier
- Vault
- Gauge

Components involved:

- `src/MediaIngest.Workflow`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`
- `deploy/dapr`

Acceptance themes:

- A package gets a durable workflow instance.
- Workflow coordinates scan, classification, processing, reconciliation, and finalization.
- Workflow runtime state is separate from business state.

Dependencies:

- USER-STORY-8

## USER-STORY-10: Support Nested Workflows

As a workflow operator, I want package workflows to drill into child workflows,
so that complex ingest processing remains understandable.

Milestone: MILESTONE-5

Domains:

- Child workflow
- Workflow graph

Ownership lanes:

- Pulse
- Canvas
- Gauge

Components involved:

- `src/MediaIngest.Workflow`
- `src/MediaIngest.Contracts`
- `src/MediaIngest.Persistence`

Acceptance themes:

- Parent workflows can start meaningful child workflows.
- Child workflows have stable identifiers and parent references.
- Workflow state can be projected for UI drilldown later.

Dependencies:

- USER-STORY-9

## USER-STORY-11: Record Agent Progress

As an operator, I want each agent to record business progress and structured
logs with `workflowInstanceId`, so that processing can be audited and
correlated.

Milestone: MILESTONE-6, MILESTONE-7

Domains:

- Observability
- Timeline
- Agent execution

Ownership lanes:

- Beacon
- Essence
- Vault
- Gauge

Components involved:

- `src/MediaIngest.Observability`
- `src/MediaIngest.Persistence`
- `src/MediaIngest.Contracts`
- specialized agent projects

Acceptance themes:

- Logs include workflow and work item correlation fields.
- Business timeline records agent state transitions.
- UI status does not depend on log scraping.

Dependencies:

- USER-STORY-7

## USER-STORY-12: Visualize Workflow Execution

As an operator, I want to see workflow nodes change color by status, so that I
can quickly understand what is pending, running, failed, or complete.

Milestone: MILESTONE-8

Domains:

- Workflow graph
- Operator UI

Ownership lanes:

- Canvas
- Pulse
- Gauge

Components involved:

- `src/MediaIngest.Api`
- `web/ingest-control-plane`
- `src/MediaIngest.Persistence`

Acceptance themes:

- Graph is rendered from a graph DTO, not a bitmap.
- Node colors come from business state.
- Pending, running, succeeded, failed, waiting, skipped, and cancelled states are distinguishable.

Dependencies:

- USER-STORY-10
- USER-STORY-11

## USER-STORY-13: Inspect Node Logs

As an operator, I want to click a workflow node and view its logs and timeline,
so that I can diagnose failures without searching raw logs manually.

Milestone: MILESTONE-8

Domains:

- Workflow node
- Timeline
- Logs

Ownership lanes:

- Canvas
- Beacon
- Vault

Components involved:

- `src/MediaIngest.Api`
- `web/ingest-control-plane`
- `src/MediaIngest.Observability`
- `src/MediaIngest.Persistence`

Acceptance themes:

- Node details show business timeline entries.
- Logs are filtered by workflow and node correlation fields.
- Failure context is visible without raw log search.

Dependencies:

- USER-STORY-11
- USER-STORY-12

## USER-STORY-14: Drill Into Nested Workflows

As an operator, I want to drill into child workflow graphs, so that nested
processing remains explorable.

Milestone: MILESTONE-8

Domains:

- Child workflow
- Workflow graph
- Operator UI

Ownership lanes:

- Canvas
- Pulse
- Gauge

Components involved:

- `src/MediaIngest.Api`
- `web/ingest-control-plane`
- `src/MediaIngest.Persistence`

Acceptance themes:

- Parent graph nodes can link to child workflow graphs.
- Child graph view preserves parent context.
- Nested workflow status is visible at parent and child levels.

Dependencies:

- USER-STORY-10
- USER-STORY-12

## USER-STORY-15: Navigate Back From Child Workflows

As an operator, I want to navigate back from child workflows to parent workflows,
so that drilldown exploration is reversible.

Milestone: MILESTONE-8

Domains:

- Workflow navigation
- Operator UI

Ownership lanes:

- Canvas
- Pulse

Components involved:

- `src/MediaIngest.Api`
- `web/ingest-control-plane`

Acceptance themes:

- UI supports back traversal from child to parent workflow.
- Navigation preserves selected package context.
- Direct links to child workflow views remain possible.

Dependencies:

- USER-STORY-14

## USER-STORY-16: Develop With Docker-First Tooling

As a developer, I want a Docker-first Linux development workflow, so that I can
work on the repository without installing every runtime SDK on the host.

Milestone: MILESTONE-1, MILESTONE-2

Domains:

- Developer experience
- Tooling

Ownership lanes:

- Forge
- Gauge

Components involved:

- `README.md`
- `Makefile`
- `package.json`
- `scripts/dev`
- `docs/standards/tooling.md`
- `docs/automation`

Acceptance themes:

- Required host tools stay minimal.
- Optional host tools are documented.
- Make targets expose canonical workflows.
- Agents update quickstart and tooling docs when workflow changes.

Dependencies:

- None

## USER-STORY-17: Deploy To Kubernetes

As a platform engineer, I want all services containerized and deployable to
Kubernetes, so that the system matches a cloud-native Azure runtime model.

Milestone: MILESTONE-2, MILESTONE-9

Domains:

- Kubernetes runtime
- Azure deployment
- Platform

Ownership lanes:

- Forge
- Shield
- Beacon

Components involved:

- `deploy/docker`
- `deploy/k8s`
- `deploy/dapr`
- `deploy/azure`
- `docs/architecture`
- `docs/automation`

Acceptance themes:

- Services are containerized.
- Kubernetes manifests or Helm assets are documented.
- Azure deployment guidance avoids paid actions by default.
- Cloud execution requires explicit approval.

Dependencies:

- MILESTONE-2 local runtime foundation.
