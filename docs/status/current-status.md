# Current Status

## Project State

- Phase: local implementation foundation.
- Current branch: `main`.
- Current focus: local `main` contains the first implementation foundations
  across watcher, persistence/outbox, workflow, command routing, observability,
  and the React control plane.

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
- Active worktree tracking and PR authorization rules created.
- GitHub Projects roadmap created with milestone epics and numbered user-story
  issues.
- Initial GitHub issue hierarchy, dependencies, issue bodies, and Project fields
  refined; later simplified to lightweight board tracking.
- Read-only GitHub tracker helper commands added for agent verification.
- Local task, bug, worktree, checklist, and definition-of-done docs aligned with
  the GitHub Projects tracker model.
- Markdown link validation, safe docs fix commands, and Git branch/commit/PR
  naming conventions were merged in PR #29.
- Task issues #30 through #37 were created for the first parallel execution
  lanes, with #30 and #32 active as the foundation worktrees.
- GitHub tracker helper commands were added for Project field updates, native
  sub-issue links, and native blocked-by dependency links.
- GitHub tracker helper commands now also cover required Project field audits,
  issue-body relationship linting, and PR creation with Project/worktree state
  updates.
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
- Merged task worktrees for TASK-3-1 through TASK-8-1 were removed and marked
  `Cleaned Up` in the local worktree mirror.
- GitHub tracker state for task issues #31 and #33 through #37 was reconciled
  to closed, `status:complete`, and Project `Done`; future tracking is
  lightweight status only.
- Command routing now uses semantic command topics and light, medium, or heavy
  execution classes instead of media-specific agent projects.
- Agent execution tooling now includes focused .NET smoke-test targets, focused
  validation targets, an agent preflight command, and a repo-local ignored
  Docker .NET cache for faster repeated validation.

## Ready For Review

- No active PR is awaiting review.

## Next

- Plan the next increment from the updated local implementation foundation and
  reconciled GitHub tracker state.

## Update Rule

Agents must update this file when a task changes the project phase, active
milestone, completed work, current focus, or next action.
