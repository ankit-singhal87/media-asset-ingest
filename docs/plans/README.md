# Plans

Planning artifacts turn product stories into executable implementation work.

## Structure

- `docs/product/milestones.md` - milestone catalog and story mapping.
- `docs/product/user-stories.md` - numbered user stories and traceability.
- `docs/plans/task-index.md` - local task-plan mirror.
- `docs/plans/parallel-system-components.md` - current parallel
  system-component track plan.
- `docs/plans/milestones/` - milestone-level implementation plans.
- `docs/plans/slices/` - cross-milestone review slice plans.
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
5. Track implementation progress in GitHub issues and PRs, with the Project as
   a lightweight status board.
6. Track defects in GitHub issues when remote visibility is needed and mirror
   durable defect details in `docs/bugs`.
7. Use `docs/automation/execution-checklist.md`, `definition-of-done.md`, and
   `agent-handoff.md` during execution.
8. Update simple GitHub status when issue, task, bug, or PR state changes.
9. Update ignored local `.worktrees/state/<worktree-slug>.md` records only when
   coordinating active parallel worktrees.

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

Use GitHub issues and PRs as durable remote tracking. Use the GitHub Project as
a lightweight status board only. Use ignored local
`.worktrees/state/<worktree-slug>.md` records for active worktree coordination.
Do not commit live worktree state or historical cleanup rows.

## Local Worktree State

Live active-worktree coordination belongs outside committed planning docs. When
parallel work is active, create one ignored local state file per worktree under
`.worktrees/state/` using the format in
`docs/automation/parallelization.md`. Delete that state file when the merged
worktree is cleaned up.
