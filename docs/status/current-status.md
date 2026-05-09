# Current Status

## Project State

- Phase: local implementation foundation.
- Current branch: `main`.
- Current focus: local `main` contains the first reviewable durable ingest
  slice across watcher, raw Npgsql/PostgreSQL persistence, transactional
  outbox, workflow graph projection, command routing, observability, runnable
  worker host processes, and the React control plane. Docker Compose now starts
  API, UI, PostgreSQL, outbox worker, and light/medium/heavy command-runner
  host containers. The local runtime smoke verifies API/UI readiness, manifest
  output, workflow command-node evidence, persisted PostgreSQL package state,
  dispatched command outbox rows grouped by execution class, and local
  command-runner service startup boundaries without Azure resources or real
  Dapr Workflow hosting.

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
- Early remote tracking was created for milestone epics and numbered user-story
  issues, then superseded by repo-doc state as the durable planning source.
- Local task, bug, checklist, and definition-of-done docs were aligned around
  repo-doc planning and status.
- Markdown link validation, safe docs fix commands, and Git branch/commit/PR
  naming conventions were merged in PR #29.
- Task issues #30 through #37 were created for the first parallel execution
  lanes, with #30 and #32 active as the foundation worktrees.
- Legacy remote board helper commands were removed; repo docs now hold
  durable state, plans, milestones, bugs, and status.
- TASK-2-1 added the initial buildable .NET solution skeleton and canonical
  `make test-dotnet` validation entrypoint.
- TASK-2-2 defined initial shared workflow graph, node detail, status, and
  workflow name contracts for backend, workflow, and UI slices.
- `make test-dotnet` now runs both foundation and contract smoke-test projects.
- TASK-3-1 added the ingest watcher scanner foundation.
- TASK-4-1 added the persistence and outbox foundation.
- TASK-5-1 added the Dapr workflow skeleton.
- TASK-6-1 initially added media-specific worker skeletons; these were
  superseded by generic command-routing contracts.
- TASK-7-1 added the observability correlation field foundation.
- TASK-8-1 scaffolded the React workflow control plane with mocked workflow
  graph data and focused UI tests.
- Merged task worktrees for TASK-3-1 through TASK-8-1 were removed; active
  worktree coordination now stays in ignored local `.worktrees/state/` records.
- The tracked active worktree ledger was removed from planning docs to avoid
  conflicts across parallel PRs.
- Early task tracking for #31 and #33 through #37 was reconciled after local
  task merges; future durable state lives in repo docs.
- Command routing now uses semantic command topics and light, medium, or heavy
  execution classes instead of media-specific command runner projects.
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
- USER-STORY-7 added a local generic command runner foundation for light,
  medium, and heavy execution classes. The runner accepts matching
  `MediaCommandEnvelope` values, rejects mismatched execution classes, skips
  duplicate `CommandId` handling, and records in-memory business progress with
  canonical correlation fields. Azure Service Bus consumption, Kubernetes
  scaling, and real media command execution remain deferred.
- USER-STORY-7 and USER-STORY-16 now have a local Azure-shaped command-runner
  consumption boundary. Broker-shaped command messages are validated against
  semantic topic and execution-class subscription metadata, deserialized into
  `MediaCommandEnvelope` values, checked for required envelope shape and route,
  handled by the generic runner, and mapped to complete, abandon, or
  dead-letter decisions without Azure SDK dependencies, secrets, or cloud
  validation.
- TASK-8-2 added multi-package workflow selection in the React control plane,
  preserving node detail during same-workflow refreshes and clearing detail when
  the operator switches workflows.
- TASK-8-3 made the Mermaid diagram the only workflow graph UI, with
  React-managed SVG node activation, compact keyboard-accessible node
  selection, child workflow drilldown, and parent back navigation.
- USER-STORY-13 now has persisted local business timeline and node diagnostic
  log backing for workflow node details, so the existing node details endpoint
  reads stored package start, command dispatch, done-marker reconciliation,
  success, and failure records instead of synthetic package-state text. This
  does not complete production log aggregation or OpenTelemetry trace linkage.
- TASK-2-3 added a Docker-first local runtime smoke target that starts the
  Compose API, UI, and PostgreSQL stack, waits for API/UI HTTP responses, runs
  the existing local ingest smoke against the containerized API with
  manifest-output assertions plus workflow command-node evidence, and stops
  the stack without cloud resources or secrets.
- TASK-5-2 expanded nested workflow behavior contracts so prepared child work
  now carries stable child workflow instance IDs and parent workflow references,
  and workflow graph projection can render queued child workflow nodes directly
  from a package workflow start.
- TASK-5-3 added the standalone `MediaIngest.Workflow.Orchestrator`
  bounded-context assembly and public marker for future catalog discovery.
  Real Dapr Workflow SDK hosting remains deferred to a later
  dependency-approved task.
- TASK-5-4 added the attribute-discovered workflow definition catalog in the
  orchestrator boundary, including package ingest topology metadata for waits,
  command dispatch/completion, child workflows, and finalization plus catalog
  validation for duplicate nodes, invalid edges, and missing metadata.
- TASK-8-5 added an orchestrator-backed workflow graph projector and wired the
  API workflow graph facade to project package graphs from catalog metadata
  overlaid with business package status and dynamic command work-item nodes.
- TASK-8-6 updated the React Mermaid workflow graph model and tests so wait,
  command dispatch, command completion, finalization, child workflow, and
  dynamic command work nodes render from orchestrator-generated DTOs while
  preserving node details, child workflow drilldown, and back navigation.
- TASK-4-4 defined the Service Bus command-bus adapter boundary in the outbox
  worker. Command publish requests now map to a broker-oriented message shape
  with semantic topic, raw command body, application properties, and routed
  light/medium/heavy subscription name before local Dapr or in-memory publisher
  delegation.
- Agent validation now has summary-only entrypoints that keep full logs under
  `/tmp` while printing command, exit code, key success or failure lines, and
  log path. Current task capsules live in ignored `.worktrees/state/` records
  for compact resume context.
- Remote board guidance and helper commands were removed. Repo docs are the
  source of truth for product state, plans, milestones, bugs, and status.
- The reviewable local durable ingest slice now uses raw Npgsql-backed
  PostgreSQL persistence in Compose, guarded local startup schema creation,
  Problem Details for not-found and conflict API responses, static OpenAPI
  documentation, runnable outbox and command-runner host processes, and a
  reviewer guide with honest local/cloud boundaries.

## Next

- Plan the later dependency-approved task for real Dapr Workflow SDK hosting
  and workflow/activity registration in the orchestrator boundary.
- Plan the later Azure Service Bus SDK worker integration after the local
  command-bus boundary and host processes are reviewed.

## Update Rule

Agents must update this file when a task changes the project phase, active
milestone, completed work, current focus, or next action.
