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
| `.worktrees/story-planning` | `docs/story-planning` | #26 | Product planning docs | Atlas | `AGENTS.md`, `README.md`, `docs/product`, `docs/plans`, `docs/bugs`, `docs/automation`, `docs/standards`, `docs/status`, `Makefile`, `package.json`, `scripts/dev` | Merged | https://github.com/ankit-singhal87/media-asset-ingest/pull/28 |
| `.worktrees/link-and-commit-standards` | `USER-STORY-16-link-and-commit-standards` | #26 | USER-STORY-16 | Forge | `AGENTS.md`, `docs/automation`, `docs/product/task-workflow.md`, `docs/status`, `docs/standards/tooling.md`, `Makefile`, `package.json`, `scripts/dev/check-docs.mjs` | Merged | https://github.com/ankit-singhal87/media-asset-ingest/pull/29 |

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
