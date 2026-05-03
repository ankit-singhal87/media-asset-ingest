# Task Workflow

## Required Flow

Each implementation task should follow:

1. Confirm scope and ownership lane.
2. Read `AGENTS.md` and `docs/automation/README.md`.
3. Read relevant architecture, ADR, product, and standards docs.
4. Read `docs/automation/execution-checklist.md`.
5. Read `docs/automation/git-conventions.md`.
6. Read or create the relevant GitHub issue and Project item.
7. Confirm GitHub parent/sub-issue and dependency relationships.
8. Use a branch name that starts with the lowest-level work identifier.
9. Write or identify a BDD scenario for user-visible behavior.
10. Write a failing test before production code.
11. Implement the smallest change that passes.
12. Refactor while tests stay green.
13. Run the validation command from `docs/automation/validation.md`.
14. Update impacted docs and GitHub Projects.
15. Prepare handoff using `docs/automation/agent-handoff.md`.
16. Report local validation and GitHub tracker verification evidence.

## Required Documentation Updates

Before a task is complete, update the relevant files:

- `docs/status/current-status.md` when phase, focus, next action, or milestone state changes.
- `docs/status/work-log.md` for meaningful completed work or bug fixes.
- `docs/product/backlog.md` when work is added, completed, split, or deferred.
- `docs/product/milestones.md` when milestone scope or progress changes.
- `docs/product/user-stories.md` when acceptance meaning changes.
- `docs/plans` when milestone plans or task plans are added, split, renamed, or deleted.
- `docs/plans/task-index.md` when local task files are added, renamed, or deleted.
- `docs/plans/active-worktrees.md` when worktree, branch, status, target files, or PR state changes.
- GitHub Projects when issue, milestone, bug, task, worktree, or PR state changes.
- `docs/bugs` when durable defect documentation is added, renamed, or deleted.
- `docs/bugs/index.md` when local bug files are added, renamed, or deleted.
- `README.md` quickstart when onboarding, setup, commands, or validation changes.
- `docs/adr` when an architectural decision changes.
- `docs/standards/tooling.md` when a required development tool changes.
- `scripts/dev/check-tools.sh` when a required tool must be detected.
- `scripts/dev/install-tools.sh` when a required tool should be installable.
- `docs/automation/commands.md` when a canonical command is added or changed.
- `docs/automation/validation.md` when validation expectations change.

## Bug Fixes

Bug fixes must include:

- the observed behavior
- the expected behavior
- a failing regression test first, unless the user approves an exception
- the fix
- validation evidence
- a GitHub issue with `type:bug`
- a `docs/bugs/BUG-<number>-<title>.md` entry
- any status, work-log, story, or milestone update
