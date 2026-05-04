# Task Index

Use this index as a local mirror for implementation task files created from
milestone plans. GitHub issues and PRs are durable remote tracking; the GitHub
Project is only a lightweight status board.

## Status Values

- Planned
- In Progress
- Blocked
- Completed
- Deferred

## Tasks

This index is a legacy local mirror for task files that exist under
`docs/plans/tasks/`. It is not an exhaustive ledger of every post-8-1 GitHub
task or merged PR: later short-lived task branches used GitHub issues, PRs,
status docs, work logs, and ignored `.worktrees/state/` records as the durable
coordination trail unless a local task file was created or renamed.

| Task ID | GitHub issue | Title | Primary lane | Task file | Local status |
| --- | --- | --- | --- | --- | --- | --- |
| TASK-2-1 | #30 | Create .NET solution skeleton | Forge | `docs/plans/tasks/TASK-2-1-create-dotnet-solution.md` | Completed |
| TASK-2-2 | #32 | Define shared backend and UI contracts | Forge | `docs/plans/tasks/TASK-2-2-shared-backend-ui-contracts.md` | Completed |
| TASK-3-1 | #35 | Add ingest watcher and scanner foundation | Mount | `docs/plans/tasks/TASK-3-1-ingest-watcher-scanner.md` | Completed |
| TASK-3-2 | none | Add integrated local ingest demo | Gauge | `docs/plans/tasks/TASK-3-2-integrated-local-ingest-demo.md` | Completed |
| TASK-4-1 | #36 | Add persistence and outbox foundation | Vault | `docs/plans/tasks/TASK-4-1-persistence-outbox-foundation.md` | Completed |
| TASK-5-1 | #31 | Add Dapr workflow skeleton | Pulse | `docs/plans/tasks/TASK-5-1-dapr-workflow-skeleton.md` | Completed |
| TASK-6-1 | #34 | Add command execution foundation | Essence | `docs/plans/tasks/TASK-6-1-specialized-agent-skeletons.md` | Completed |
| TASK-7-1 | #37 | Add observability correlation foundation | Beacon | `docs/plans/tasks/TASK-7-1-observability-correlation.md` | Completed |
| TASK-8-1 | #33 | Scaffold React workflow control plane | Canvas | `docs/plans/tasks/TASK-8-1-react-control-plane.md` | Completed |

## Update Rule

Agents must update this index when local task files are created, renamed, or
deleted. Update GitHub issues, native relationships, PRs, or simple Project
status only when remote tracker state changes.
