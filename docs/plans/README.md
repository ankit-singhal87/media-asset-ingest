# Plans

Planning artifacts turn product stories into executable implementation work.

## Structure

- `docs/product/milestones.md` - milestone catalog and story mapping.
- `docs/product/user-stories.md` - numbered user stories and traceability.
- `docs/plans/task-index.md` - local task-plan mirror; GitHub Project task issues are the tracker source of truth.
- `docs/plans/active-worktrees.md` - local active worktree and parallel-agent coordination mirror.
- `docs/plans/milestones/` - milestone-level implementation plans.
- `docs/plans/tasks/` - small task files created from milestone plans.
- `docs/bugs/` - defect reports and bug-fix tracking.
- `docs/plans/templates/` - copyable plan and task templates.

## ID Scheme

- Milestones: `MILESTONE-1`, `MILESTONE-2`, and so on.
- User stories: `USER-STORY-1`, `USER-STORY-2`, and so on.
- Tasks: `TASK-<milestone>-<number>`, for example `TASK-2-1`.
- Bugs: `BUG-1`, `BUG-2`, and so on.

## Plan Folder Workflow

1. Keep product intent in `docs/product/user-stories.md`.
2. Map each story to milestone, domain, components, owner lane, and status.
3. Create a milestone plan in `docs/plans/milestones/` before implementation.
4. Split milestone plans into task files in `docs/plans/tasks/`.
5. Track implementation progress in GitHub Projects and mirror durable plan details in task files, milestone plans, product docs, and status docs.
6. Track defects in GitHub Issues/Projects and mirror durable defect details in `docs/bugs`.
7. Use `docs/automation/execution-checklist.md`, `definition-of-done.md`, and
   `agent-handoff.md` during execution.
8. Update GitHub Projects when issue, milestone, task, bug, or PR state changes.
9. Update `docs/plans/active-worktrees.md` when working in parallel worktrees.

Use [templates/milestone-plan-template.md](templates/milestone-plan-template.md)
and [templates/task-template.md](templates/task-template.md) for new plan files.

## Task File Expectations

Each task file should include durable execution context that is not already
native GitHub metadata:

- task ID
- linked user story IDs
- GitHub issue number
- ownership lane
- target files
- BDD scenario or acceptance behavior
- RED/GREEN/REFACTOR evidence expectations
- validation command
- required documentation updates
- completion checklist

Do not create task files without a clear owning GitHub issue and validation
path.

## Parallel Execution

Use `docs/automation/parallelization.md` before assigning tasks to parallel
agents. Parallel tasks must have disjoint target files and clear handoff
expectations.

Use GitHub Projects as the human-facing tracker for issues, milestones, tasks,
bugs, and PR state. Use `docs/plans/active-worktrees.md` only as the local
ledger for active worktrees, branches, target files, and immediate coordination.
