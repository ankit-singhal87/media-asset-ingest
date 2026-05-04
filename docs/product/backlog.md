# Backlog

Backlog items should reference user stories and milestones. Keep this file at
epic/story granularity; detailed implementation tasks belong in plans.

## Recently Completed Foundations

- MILESTONE-2 / USER-STORY-16: scaffold .NET solution, shared contracts, and
  canonical .NET validation.
- MILESTONE-2 / USER-STORY-16: keep the draft local manifest ingest demo docs
  aligned with the backend and UI draft PRs.
- MILESTONE-3 / USER-STORY-1: add ingest watcher scanner foundation.
- MILESTONE-3 / USER-STORY-3: enumerate every physical file under a ready
  package directory without wiring discovery into copy behavior.
- MILESTONE-4 / USER-STORY-8: add persistence and transactional outbox
  foundation.
- MILESTONE-5 / USER-STORY-9: add Dapr workflow skeleton.
- MILESTONE-6 / USER-STORY-7: replace media-specific agent skeletons with
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
- MILESTONE-8 / USER-STORY-12 / USER-STORY-13: add package workflow selection
  to the control plane and preserve or clear node detail state according to the
  selected workflow.
- MILESTONE-8 / USER-STORY-13 / USER-STORY-14 / USER-STORY-15: make the
  Mermaid workflow diagram interactive, remove the duplicated node-card graph,
  and support child workflow drilldown with parent back navigation.

## Ready For Planning

- MILESTONE-2 / USER-STORY-17: define local container runtime and Kubernetes
  readiness boundary.
- MILESTONE-2 / USER-STORY-16: merge the local ingest docs after the backend and
  UI draft PRs provide the documented API host, UI start action, and runtime
  ignore setup.
- MILESTONE-4 / USER-STORY-6: define Azure Service Bus topic/subscription
  adapters and local development strategy.
- MILESTONE-5 / USER-STORY-10: expand nested workflow contracts and behavior.

## Later

- MILESTONE-6 / USER-STORY-7: implement light, medium, and heavy command
  runner services.
- MILESTONE-8 / USER-STORY-12 / USER-STORY-13: expand workflow graph data with
  node detail, timeline, and log-backed status sources.
- MILESTONE-9 / USER-STORY-17: add Kubernetes and Dapr deployment assets.
- MILESTONE-9 / USER-STORY-17: add Azure deployment documentation.

## Update Rule

Agents must update this file when adding, completing, splitting, or deferring
product-visible work.
