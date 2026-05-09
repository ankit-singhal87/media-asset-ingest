# Backlog

Backlog items should reference user stories and milestones. Keep this file at
epic/story granularity; detailed implementation tasks belong in plans.

## Recently Completed Foundations

- MILESTONE-2 / USER-STORY-16: scaffold .NET solution, shared contracts, and
  canonical .NET validation.
- MILESTONE-2 / USER-STORY-16: keep the local manifest ingest demo, local
  Compose validation, and Docker-first quickstart docs aligned with merged
  runtime support.
- MILESTONE-3 / USER-STORY-1: add ingest watcher scanner foundation.
- MILESTONE-3 / USER-STORY-3: enumerate every physical file under a ready
  package directory without wiring discovery into copy behavior.
- MILESTONE-4 / USER-STORY-8: add persistence and transactional outbox
  foundation.
- MILESTONE-5 / USER-STORY-9: add Dapr workflow skeleton.
- MILESTONE-6 / USER-STORY-7: replace media-specific worker skeletons with
  command-routing contracts.
- MILESTONE-7 / USER-STORY-11: add observability correlation field foundation.
- MILESTONE-8 / USER-STORY-12: scaffold React workflow control plane with
  mocked workflow graph data.
- MILESTONE-8 / USER-STORY-12: add the first workflow graph API and Mermaid
  control-plane rendering slice.
- MILESTONE-3/4/5/8: integrate the local ingest demo so discovered files create
  routed command outbox messages and real workflow command nodes.
- MILESTONE-3 / USER-STORY-4: reconcile on `done.marker` so packages remain
  non-terminal until a zero-byte marker appears and late files are included in
  command outbox work.
- MILESTONE-4 / USER-STORY-6 / USER-STORY-8: expose broker-ready command
  publish metadata at the local outbox boundary without changing non-command
  dispatch behavior.
- MILESTONE-4 / USER-STORY-6 / USER-STORY-8: define static command-bus topology
  readiness for semantic Azure Service Bus command topics and light, medium,
  and heavy subscriptions.
- MILESTONE-4 / USER-STORY-6 / USER-STORY-8: define the Service Bus
  command-bus adapter boundary and preserve local Dapr or in-memory publisher
  behavior through the outbox publisher strategy.
- MILESTONE-6 / USER-STORY-7: add the first local generic command runner
  foundation for light, medium, and heavy execution classes without Azure
  Service Bus consumption or real media command execution.
- MILESTONE-8 / USER-STORY-12 / USER-STORY-13: add package workflow selection
  to the control plane and preserve or clear node detail state according to the
  selected workflow.
- MILESTONE-8 / USER-STORY-13 / USER-STORY-14 / USER-STORY-15: make the
  Mermaid workflow diagram interactive, remove the duplicated node-card graph,
  and support child workflow drilldown with parent back navigation.
- MILESTONE-9 / USER-STORY-17: add static Kubernetes and Dapr readiness assets
  for internal API/UI services, PostgreSQL, workflow state, and Azure Service
  Bus command-topic placeholders without running cloud validation.
- MILESTONE-8 / USER-STORY-13: add persisted local business timeline and node
  diagnostic log backing for workflow node details, covering package start,
  command dispatch, done-marker reconciliation, success, and failure details.
- MILESTONE-5 / USER-STORY-10: expand nested workflow behavior contracts so
  prepared child workflow work has stable child identifiers, parent workflow
  references, and graph projection from package workflow start state.
- MILESTONE-5 / USER-STORY-9 / USER-STORY-10: add the standalone workflow
  orchestrator bounded-context assembly and public marker for future catalog
  discovery, while deferring real Dapr Workflow SDK registration to a later
  dependency-approved task.
- MILESTONE-5 / USER-STORY-9 / USER-STORY-10: add attribute-discovered
  orchestrator workflow topology metadata and validation for package ingest
  nodes, waits, command dispatch/completion, child workflows, and finalization.
- MILESTONE-8 / USER-STORY-12 / USER-STORY-14 / USER-STORY-15: generate API
  workflow graph DTOs from orchestrator-owned topology metadata, with dynamic
  command work-item nodes overlaid from persisted command outbox envelopes.

## Ready For Planning

- MILESTONE-2 / USER-STORY-16: plan the next Docker-first local runtime
  validation slice beyond static Compose checks.
- MILESTONE-5 / USER-STORY-9 / USER-STORY-10 and MILESTONE-8 /
  USER-STORY-12 through USER-STORY-15: continue the workflow orchestrator graph
  discovery stack with TASK-8-6.
## Later

- MILESTONE-6 / USER-STORY-7: connect generic command runners to Azure Service
  Bus subscriptions and real media command execution.
- MILESTONE-8 / USER-STORY-13: expand node diagnostics beyond local persisted
  records into production log aggregation and OpenTelemetry trace linkage.
- MILESTONE-9 / USER-STORY-17: add Azure deployment documentation.

## Update Rule

Agents must update this file when adding, completing, splitting, or deferring
product-visible work.
