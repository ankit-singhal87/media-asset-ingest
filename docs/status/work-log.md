# Work Log

Use this log for concise human-readable progress notes. Do not duplicate commit
messages or paste command output unless it explains a decision.

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
- Added GitHub sub-issue hierarchy, blocked-by dependencies, richer issue
  bodies, and populated Project fields for type, lane, and status.
- Removed duplicated relationship metadata from GitHub issue bodies and added
  read-only Make/npm helpers for GitHub tracker verification.
- Cleaned local plan, bug, worktree, checklist, and definition-of-done docs so
  GitHub Projects is the tracker source of truth and repo docs are durable
  context/mirrors.
- Tightened PR checklist and active worktree mirror after GitHub Projects
  cleanup.
- Created the first parallel execution task issues and local task files for
  .NET foundation, shared contracts, Dapr workflow, React UI, watcher,
  persistence/outbox, observability, and specialized agent lanes.
- Added tested GitHub tracker helper commands for Project field updates,
  native sub-issue links, and native blocked-by dependency links.
- Added tested GitHub tracker helpers for required field audits, issue-body
  relationship linting, and PR creation with Project/worktree state updates.

## Update Rule

Agents should add a dated bullet when completing a meaningful task, fixing a
bug, changing development workflow, or adding a new tool requirement.
