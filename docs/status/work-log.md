# Work Log

Use this log for concise human-readable progress notes. Do not duplicate commit
messages or paste command output unless it explains a decision.

## 2026-05-04

- Replaced the tracked active worktree ledger with ignored local
  `.worktrees/state/<worktree-slug>.md` records and updated planning and
  automation docs to keep worktree state out of commits.
- Added the parallel track 07 Pulse workflow boundary with in-process package
  lifecycle states for observed, ready, started, succeeded, and failed.
- Added the Track 08 Beacon runtime diagnostic event-name catalog for scan,
  readiness, copy, outbox dispatch, success, and failure observability records.
- Added the parallel Essence checksum validator component with focused tests for
  valid, missing, malformed, and mismatched raw SHA-256 manifest checksums.

## 2026-05-03

- Added Markdown link validation and Git naming conventions for branch, commit,
  and PR traceability.
- Added `make docs-fix` / `npm run docs:fix` for safe documentation formatting
  cleanup before commits.
- Created planning worktree `docs/story-planning`.
- Added architecture, ADR, product, automation, and standards documentation.
- Added repository operating model for status, task workflow, and toolchain checks.
- Added BDD/TDD, DDD, and tooling standards.
- Added Linux development tool check/install scripts.
- Added Makefile and package.json entrypoints for validation.
- Added README quickstart and Docker-first tooling direction.
- Added milestone and user story IDs with domain/component/lane mappings.
- Added `docs/plans` and `docs/bugs` folder structures.
- Added task/bug templates, indexes, execution checklist, handoff format,
  definition of done, parallelization rules, PR checklist, and conflict protocol.
- Added active worktree tracking and automatic PR authorization rules for
  parallel Codex execution.
- Created GitHub Project `Media Asset Ingest Roadmap`, GitHub milestones,
  milestone epic issues, user-story issues, and tracker labels.
- Added initial GitHub sub-issue hierarchy, blocked-by dependencies, richer
  issue bodies, and populated Project fields for type, lane, and status.
- Removed duplicated relationship metadata from GitHub issue bodies and added
  read-only Make/npm helpers for GitHub tracker verification.
- Cleaned local plan, bug, checklist, and definition-of-done docs so GitHub
  issues and PRs carry durable tracker history while repo docs keep durable
  context.
- Tightened PR checklist and active worktree coordination after GitHub Projects
  cleanup.
- Created the first parallel execution task issues and local task files for
  .NET foundation, shared contracts, Dapr workflow, React UI, watcher,
  persistence/outbox, observability, and command execution lanes.
- Added tested GitHub tracker helper commands for Project field updates,
  native sub-issue links, and native blocked-by dependency links.
- Added tested GitHub tracker helpers for required field audits, issue-body
  relationship linting, and PR creation with Project plus local worktree-state
  updates.
- Documented serialized GitHub Project operations so parallel agents do not
  exhaust the shared GraphQL rate-limit budget.
- Added the TASK-2-1 .NET solution skeleton with a dependency-free foundation
  smoke test and canonical `make test-dotnet` / `npm run dotnet:test`
  validation entrypoints.
- Added shared workflow graph, node detail, node status, and workflow name
  contracts for backend/workflow/UI coordination in TASK-2-2.
- Extended `make test-dotnet` to execute the contract smoke-test project after
  the solution build.
- Removed completed TASK-2-1 and TASK-2-2 blocker references from downstream
  task plans after clearing the native GitHub dependencies.
- Completed TASK-3-1 ingest watcher scanner foundation with package-directory
  candidate discovery under the configured ingest mount.
- Added TASK-4-1 persistence and outbox foundation with a shared persistence
  batch for business state and outbox messages plus a dispatcher for pending
  outbox records.
- Added the TASK-5-1 workflow skeleton that starts a package workflow from an
  accepted package ingest request and prepares the initial scan, classify, and
  dispatch child work plan.
- Added TASK-6-1 specialized video, audio, text, and other agent skeletons with
  smoke tests proving each worker owns only its assigned media category.
- Added TASK-7-1 observability correlation field foundation with a canonical
  field catalog, correlation context, and smoke-test coverage in
  `make test-dotnet`.
- Scaffolded the React workflow control plane for TASK-8-1 with mocked
  workflow graph data, accessible node status labels, and focused Vitest
  coverage for the operator graph scenario.
- Reconciled local status, backlog, milestone, and user-story docs after the
  TASK-3-1 through TASK-8-1 local merges.
- Reconciled GitHub task issues and Project fields for #31 and #33 through #37
  after the local task merges.
- Simplified future GitHub tracking to issues, PRs, parent/child navigation when
  useful, and simple Project status only.
- Replaced the media-specific agent skeleton direction with generic command
  routing contracts and light, medium, or heavy execution classes.
- Added focused .NET smoke-test targets, focused validation targets, an agent
  preflight command, and a repo-local ignored Docker .NET cache to reduce
  repeated agent validation time.
- Added a dedicated optional host-tool installer for .NET SDK, kubectl, Helm,
  Dapr CLI, and Azure CLI with confirmation and post-install verification.
- Drafted the local manifest ingest demo workflow in the README, including the
  fixed local API port, UI `/api` proxy expectation, **Start ingest** watcher
  sequence, `input/<asset>/manifest.json` plus checksum setup, expected
  `output/<asset>/` manifest files, and the in-process scope boundary before
  real Dapr, PostgreSQL, and Azure Service Bus integration.
- Updated PR #45 local manifest ingest behavior so package discovery observes
  all immediate child directories, readiness requires both manifest files, and
  start ingest launches a background watcher loop.
- Refined the Courier local outbox dispatch shape so pending messages are
  published, marked with an injectable dispatch timestamp, skipped on later
  dispatcher runs, and left pending when publication fails.
- 2026-05-04: Added a Gauge local manifest ingest E2E smoke script with dry-run
  validation docs and a manual API-plus-script command path.

## 2026-05-04

- Added Mount file discovery for USER-STORY-3 so the watcher enumerates every
  physical file under a ready package directory with stable package-relative
  paths, without wiring discovery into copy, persistence, or workflow behavior.
- Added the USER-STORY-12 Mermaid workflow graph slice: workflow lifecycle
  state now projects to graph DTO statuses, `/api/workflows/{id}/graph` serves
  those DTOs, and the React control plane renders Mermaid diagrams from live
  graph data with focused API, workflow, and UI tests.
- Added USER-STORY-17 static Kubernetes and Dapr readiness assets with
  ClusterIP-only API/UI/PostgreSQL manifests, Dapr workflow state and Azure
  Service Bus component templates, placeholder-only secret examples, and
  architecture/automation docs that keep cloud validation approval-gated.

## Update Rule

Agents should add a dated bullet when completing a meaningful task, fixing a
bug, changing development workflow, or adding a new tool requirement.
