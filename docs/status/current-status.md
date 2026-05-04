# Current Status

## Project State

- Phase: local implementation foundation.
- Current branch: `main`.
- Current focus: local `main` contains the first implementation foundations
  across watcher, persistence/outbox, workflow, command routing, observability,
  and the React control plane. The local ingest demo now waits for a zero-byte
  `done.marker` before terminal success, routes command publish metadata for
  local outbox dispatch, and lets the UI select among multiple package workflow
  graphs.

## Completed

- Initial architecture direction selected.
- Azure Service Bus selected for messaging.
- PostgreSQL selected for business state and outbox.
- Dapr Workflow selected for orchestration.
- Message-centric orchestration selected.
- Automation docs and ownership lanes created.
- Product stories and milestones created.
- BDD/TDD, DDD, and tooling standards created.
- Linux tool check/install scripts created.
- Makefile and npm validation entrypoints created.
- README quickstart added with Linux-first and Docker-first guidance.
- Milestones and user stories assigned stable IDs.
- Planning and bug folder structure created.
- Agent execution templates, indexes, checklists, handoff, parallelization, and
  conflict guidance created.
- Active worktree coordination and PR authorization rules created.
- GitHub Projects roadmap created with milestone epics and numbered user-story
  issues.
- Initial GitHub issue hierarchy, dependencies, issue bodies, and Project fields
  refined; later simplified to lightweight board tracking.
- Read-only GitHub tracker helper commands added for agent verification.
- Local task, bug, checklist, and definition-of-done docs aligned with the
  lightweight GitHub tracker model.
- Markdown link validation, safe docs fix commands, and Git branch/commit/PR
  naming conventions were merged in PR #29.
- Task issues #30 through #37 were created for the first parallel execution
  lanes, with #30 and #32 active as the foundation worktrees.
- GitHub tracker helper commands were added for Project field updates, native
  sub-issue links, and native blocked-by dependency links.
- GitHub tracker helper commands now also cover required Project field audits,
  issue-body relationship linting, and PR creation with Project plus local
  worktree-state updates.
- TASK-2-1 added the initial buildable .NET solution skeleton and canonical
  `make test-dotnet` validation entrypoint.
- TASK-2-2 defined initial shared workflow graph, node detail, status, and
  workflow name contracts for backend, workflow, and UI slices.
- `make test-dotnet` now runs both foundation and contract smoke-test projects.
- TASK-3-1 added the ingest watcher scanner foundation.
- TASK-4-1 added the persistence and outbox foundation.
- TASK-5-1 added the Dapr workflow skeleton.
- TASK-6-1 initially added media-specific agent worker skeletons; these were
  superseded by generic command-routing contracts.
- TASK-7-1 added the observability correlation field foundation.
- TASK-8-1 scaffolded the React workflow control plane with mocked workflow
  graph data and focused UI tests.
- Merged task worktrees for TASK-3-1 through TASK-8-1 were removed; active
  worktree coordination now stays in ignored local `.worktrees/state/` records.
- The tracked active worktree ledger was removed from planning docs to avoid
  conflicts across parallel PRs.
- GitHub tracker state for task issues #31 and #33 through #37 was reconciled
  to closed, `status:complete`, and Project `Done`; future tracking is
  lightweight status only.
- Command routing now uses semantic command topics and light, medium, or heavy
  execution classes instead of media-specific agent projects.
- Agent execution tooling now includes focused .NET smoke-test targets, focused
  validation targets, an agent preflight command, and a repo-local ignored
  Docker .NET cache for faster repeated validation.
- README and automation docs document the current local manifest ingest flow:
  run the API host on the fixed development port, run the UI with `/api`
  proxied to that API, press **Start ingest** to begin watching, then add
  `manifest.json` plus `manifest.json.checksum` under `input/<asset>/` and
  expect both manifest files under `output/<asset>/`.
- USER-STORY-2 manifest readiness now keeps package observation separate from
  start readiness and requires both `manifest.json` and
  `manifest.json.checksum` before package work may begin.
- USER-STORY-3 physical file enumeration now scans ready package directories
  recursively in the watcher without wiring discovery into copy behavior.
- USER-STORY-5 essence classification now maps discovered media extensions to
  video/source, text, audio, or other categories, and watcher discovery exposes
  file-size metadata for routing policy inputs.
- USER-STORY-12 now has a Mermaid-backed control-plane graph path: the API
  exposes workflow graph DTOs by workflow instance, workflow lifecycle state
  projects to graph node statuses, and the React UI renders Mermaid diagrams
  while keeping package status and local start controls intact.
- USER-STORY-17 added static Kubernetes and Dapr readiness assets for the API,
  UI, PostgreSQL dependency, internal service networking, workflow state, and
  Azure Service Bus pub/sub placeholders without running cloud validation.
- TASK-3-2 integrated the local ingest demo across watcher discovery, essence
  classification, command routing, the in-memory outbox, workflow graph
  projection, and the local smoke script. Ready packages now create routed
  command envelopes for every non-metadata file and expose those command nodes
  through the workflow graph API.
- TASK-3-3 added done-marker reconciliation so manifest-ready packages can begin
  work before `done.marker`, remain non-terminal while waiting, rescan on a
  zero-byte marker, and enqueue late discovered files idempotently.
- TASK-4-2 added local command dispatch boundary metadata so
  `MediaCommandEnvelope` outbox publishes expose broker-ready `executionClass`
  application properties while non-command messages keep existing behavior.
- TASK-4-3 defined the static command-bus topology readiness model for semantic
  command topics and the `light`, `medium`, and `heavy` execution-class
  subscriptions without provisioning Azure resources.
- TASK-8-2 added multi-package workflow selection in the React control plane,
  preserving node detail during same-workflow refreshes and clearing detail when
  the operator switches workflows.
- TASK-8-3 made the Mermaid diagram the only workflow graph UI, with
  React-managed SVG node activation, compact keyboard-accessible node
  selection, child workflow drilldown, and parent back navigation.

## Next

- Push or open a PR for the local `main` integration when authorized.
- Plan the next runtime slice: richer node log/timeline data sources or local
  container runtime validation.

## Update Rule

Agents must update this file when a task changes the project phase, active
milestone, completed work, current focus, or next action.
