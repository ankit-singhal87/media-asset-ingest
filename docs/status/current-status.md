# Current Status

## Project State

- Phase: planning and repository foundation.
- Current branch: `main`.
- Current focus: MILESTONE-2 .NET solution foundation and shared contract
  setup for parallel backend, workflow, and UI work.

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
- GitHub issue hierarchy, dependencies, issue bodies, and Project fields
  refined.
- Read-only GitHub tracker helper commands added for agent verification.
- Local task, bug, worktree, checklist, and definition-of-done docs aligned with
  the GitHub Projects tracker model.
- Markdown link validation, safe docs fix commands, and Git branch/commit/PR
  naming conventions were merged in PR #29.
- Task issues #30 through #37 were created for the first parallel execution
  lanes, with #30 and #32 active as the foundation worktrees.
- GitHub tracker helper commands now cover Project field updates, native
  sub-issue links, and native blocked-by dependency links.

## Ready For Review

- No active PR is awaiting review.

## Next

- Complete TASK-2-1 and TASK-2-2 before starting contract-consuming parallel
  implementation worktrees.

## Known Agent Execution Gaps

- Project custom fields are populated for current issues, but helper scripts do
  not verify required field completeness across all items.
- There is no automated check that issue bodies avoid duplicated relationship
  metadata.
- There is no standardized PR creation helper that updates GitHub Project fields
  and `docs/plans/active-worktrees.md` together.

## Update Rule

Agents must update this file when a task changes the project phase, active
milestone, completed work, current focus, or next action.
