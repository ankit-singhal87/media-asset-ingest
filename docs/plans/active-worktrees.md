# Active Worktrees

Use this file to coordinate multiple Codex processes working in parallel.
GitHub Projects remains the tracker source of truth for issues, milestones,
tasks, bugs, and PR state; this file is only the local worktree coordination
mirror.

## Status Values

- Planned
- Active
- Blocked
- Ready For PR
- PR Open
- Merged
- Cleaned Up

## Active Work

| Worktree | Branch | GitHub item | Stories | Lane | Target files | Status | PR |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `.worktrees/story-planning` | `docs/story-planning` | #26 | Product planning docs | Atlas | `AGENTS.md`, `README.md`, `docs/product`, `docs/plans`, `docs/bugs`, `docs/automation`, `docs/standards`, `docs/status`, `Makefile`, `package.json`, `scripts/dev` | Cleaned Up | https://github.com/ankit-singhal87/media-asset-ingest/pull/28 |
| `.worktrees/link-and-commit-standards` | `USER-STORY-16-link-and-commit-standards` | #26 | USER-STORY-16 | Forge | `AGENTS.md`, `docs/automation`, `docs/product/task-workflow.md`, `docs/status`, `docs/standards/tooling.md`, `Makefile`, `package.json`, `scripts/dev/check-docs.mjs` | Cleaned Up | https://github.com/ankit-singhal87/media-asset-ingest/pull/29 |
| `.worktrees/TASK-2-1-create-dotnet-solution` | `TASK-2-1-create-dotnet-solution` | #30 | USER-STORY-16 | Forge | `MediaIngest.sln`, `src`, `tests`, `Makefile`, `package.json`, `scripts/dev`, `docs/automation/commands.md`, `docs/automation/validation.md`, `docs/standards/tooling.md`, `docs/status`, `docs/plans` | Cleaned Up | https://github.com/ankit-singhal87/media-asset-ingest/pull/39 |
| `.worktrees/TASK-2-2-shared-contracts` | `TASK-2-2-shared-contracts` | #32 | USER-STORY-16 | Forge | `MediaIngest.sln`, `src/MediaIngest.Contracts`, `tests/MediaIngest.Contracts.Tests`, `scripts/dev/test-dotnet.sh`, `docs/architecture`, `docs/status`, `docs/plans` | Cleaned Up | Local merge to `main` |
| `.worktrees/TASK-3-1-ingest-watcher-scanner` | `TASK-3-1-ingest-watcher-scanner` | #35 | USER-STORY-1 | Mount | `src/MediaIngest.Worker.Watcher`, `tests/MediaIngest.Worker.Watcher.Tests`, `MediaIngest.sln`, `scripts/dev/test-dotnet.sh`, `docs/plans/task-index.md`, `docs/plans/active-worktrees.md`, `docs/status/work-log.md` | Merged | Local merge to `main` |
| `.worktrees/TASK-4-1-persistence-outbox-foundation` | `TASK-4-1-persistence-outbox-foundation` | #36 | USER-STORY-8 | Vault | `src/MediaIngest.Persistence`, `src/MediaIngest.Worker.Outbox`, `tests/MediaIngest.Persistence.Tests`, `tests/MediaIngest.Worker.Outbox.Tests`, `docs/plans/task-index.md`, `docs/plans/active-worktrees.md`, `docs/status/work-log.md`, `MediaIngest.sln`, `scripts/dev/test-dotnet.sh` | Merged | Local merge to `main` |
| `.worktrees/TASK-5-1-dapr-workflow-skeleton` | `TASK-5-1-dapr-workflow-skeleton` | #31 | USER-STORY-9 | Pulse | `MediaIngest.sln`, `src/MediaIngest.Workflow`, `tests/MediaIngest.Workflow.Tests`, `deploy/dapr`, `scripts/dev/test-dotnet.sh`, `docs/plans/task-index.md`, `docs/plans/active-worktrees.md`, `docs/status/work-log.md` | Merged | Local merge to `main` |
| `.worktrees/TASK-6-1-specialized-agent-skeletons` | `TASK-6-1-specialized-agent-skeletons` | #34 | USER-STORY-7 | Essence | `src/MediaIngest.Agents.Video`, `src/MediaIngest.Agents.Audio`, `src/MediaIngest.Agents.Text`, `src/MediaIngest.Agents.Other`, `tests/MediaIngest.Agents.*.Tests`, `MediaIngest.sln`, `scripts/dev/test-dotnet.sh`, `docs/plans/task-index.md`, `docs/plans/active-worktrees.md`, `docs/status/work-log.md` | Merged | Local merge to `main` |
| `.worktrees/TASK-7-1-observability-correlation` | `TASK-7-1-observability-correlation` | #37 | USER-STORY-11 | Beacon | `src/MediaIngest.Observability`, `tests/MediaIngest.Observability.Tests`, `MediaIngest.sln`, `scripts/dev/test-dotnet.sh`, `docs/architecture/004-observability-and-ui.md`, `docs/plans/task-index.md`, `docs/plans/active-worktrees.md`, `docs/status/work-log.md` | Merged | Local merge to `main` |

## Update Rule

Agents must update this file when:

- creating a worktree
- starting a task in a worktree
- changing target files
- marking work ready for PR
- opening a PR
- merging a PR
- cleaning up a worktree

Agents must also update GitHub Projects when tracker state changes. Do not use
this file as the only record of issue, task, bug, milestone, or PR state.
When a PR merges, the owning agent must remove its local worktree and mark the
row `Cleaned Up`, unless cleanup is blocked and reported in the handoff.

## Parallel Safety Rule

Before starting work in a new worktree, check this file and
`docs/automation/parallelization.md`. If target files overlap with active work,
stop and use `docs/automation/conflict-protocol.md`.
