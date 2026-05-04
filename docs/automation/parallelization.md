# Parallelization

Parallel agents are allowed only when file ownership is disjoint and task
dependencies do not require sequential execution.

Use GitHub issues and PRs for durable work tracking. Use the GitHub Project as
a lightweight status board only. Use ignored local
`.worktrees/state/<worktree-slug>.md` records for active parallel Codex
processes.

`.worktrees/` is gitignored. Live state under `.worktrees/state/` must not be
committed. Git history, GitHub issues, and PR links are the durable record after
work completes.

## Safe Parallel Patterns

| Lane | Usually safe with | Avoid parallel edits with |
| --- | --- | --- |
| Atlas | Canvas, Beacon, Gauge | Pulse when workflow boundaries are changing |
| Mount | Canvas, Beacon, Forge | Pulse, Courier, Vault on shared contracts |
| Pulse | Canvas after API contracts stabilize | Mount, Courier, Vault on workflow contracts |
| Courier | Canvas, Beacon | Pulse, Vault on message/outbox contracts |
| Vault | Canvas after API contracts stabilize | Courier, Pulse on persistence contracts |
| Essence | Canvas, Beacon | Courier on command contracts |
| Beacon | Most lanes for instrumentation docs | Any lane editing the same observability helpers |
| Canvas | Backend lanes after API DTOs stabilize | Pulse/Beacon when graph/log APIs are changing |
| Forge | Atlas, Gauge | Runtime tasks editing the same Makefile/deploy files |
| Gauge | Most lanes for test plans | Any lane editing the same tests |
| Shield | Review-only in parallel | Any task requiring active security changes |

## Required Parallel Agent Prompt Data

Each parallel agent must receive:

- task ID
- linked story IDs
- ownership lane
- allowed target files
- files it must not edit
- validation command
- expected handoff format
- requirement to update `.worktrees/state/<worktree-slug>.md` while the worktree is active
- requirement to clean up its own local worktree after the PR merges
- requirement to update simple GitHub status when tracker state changes

## Local State Record

Create one ignored local state file per active worktree:
`.worktrees/state/<worktree-slug>.md`.

Required fields:

- worktree path
- branch
- linked issue or PR
- story/task
- ownership lane
- target files
- forbidden files
- status
- validation command
- cleanup notes

Before starting another parallel task, inspect `.worktrees/state/*.md` and
`git worktree list` to confirm no active worktree owns the same files.

## Conflict Protocol

If two agents need the same file:

1. Stop the later task.
2. Report both task IDs and overlapping files.
3. Re-slice one task or serialize the tasks.
4. Update task files if the ownership boundary changes.

Do not merge competing edits manually without a new plan.

## Worktree Lifecycle

1. Create worktree from current `main`.
2. Create or update `.worktrees/state/<worktree-slug>.md`.
3. Execute only the assigned task scope.
4. Update only simple GitHub Project status when visibility needs it.
5. Mark the local state `Ready For PR` after validation passes.
6. Create PR when authorized.
7. Mark simple GitHub Project status and local state `PR Open` when tracking it.
8. After merge, the owning agent must remove its local worktree, then delete
   the matching `.worktrees/state/<worktree-slug>.md` file.

Parallel agents normally run in separate terminals with separate worktrees. Each
agent owns cleanup for the worktree it created; do not leave merged worktrees
behind unless cleanup is blocked, and report the blocker in the handoff.

## Tracker Coordination

GitHub Project commands must be serialized across terminals. Parallel agents may
work on local files at the same time, but they must not run Project status
writes or bulk tracker validation concurrently.

Recommended pattern:

1. One coordinator runs tracker checks before dispatch.
2. Each agent works locally and validates locally.
3. Agents update local docs and prepare handoff.
4. The coordinator serially updates simple GitHub status and opens PRs, or one
   agent at a time performs those steps after confirming no other tracker
   command is running.
5. The coordinator runs one lightweight tracker check after tracker writes complete.

Prefer the GitHub plugin for structured repository, issue, PR, review, diff,
commit, CI, comment, label, and merge operations. Reserve `gh project`,
`make github-project-*`, and `scripts/dev/github-projects.sh` for checkpoint
updates because those commands consume the shared GitHub GraphQL budget.
