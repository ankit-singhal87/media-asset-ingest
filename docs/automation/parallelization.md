# Parallelization

Parallel agents are allowed only when file ownership is disjoint and task
dependencies do not require sequential execution.

Use GitHub Projects as the tracker source of truth and
`docs/plans/active-worktrees.md` as the local coordination mirror for parallel
Codex processes.

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
- requirement to update `docs/plans/active-worktrees.md`
- requirement to clean up its own local worktree after the PR merges
- requirement to update GitHub Projects when tracker state changes

## Conflict Protocol

If two agents need the same file:

1. Stop the later task.
2. Report both task IDs and overlapping files.
3. Re-slice one task or serialize the tasks.
4. Update task files if the ownership boundary changes.

Do not merge competing edits manually without a new plan.

## Worktree Lifecycle

1. Create worktree from current `main`.
2. Add or update the worktree entry in `docs/plans/active-worktrees.md`.
3. Execute only the assigned task scope.
4. Update GitHub Project fields for active work, status, target files, and validation when needed.
5. Mark the local row `Ready For PR` after validation passes.
6. Create PR when authorized.
7. Mark the GitHub Project item and local row `PR Open`.
8. After merge, the owning agent must remove its local worktree, then update
   `docs/plans/active-worktrees.md` to `Cleaned Up`.

Parallel agents normally run in separate terminals with separate worktrees. Each
agent owns cleanup for the worktree it created; do not leave merged worktrees
behind unless cleanup is blocked, and report the blocker in the handoff.
