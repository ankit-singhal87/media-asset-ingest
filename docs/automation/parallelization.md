# Parallelization

Parallel agents are allowed only when file ownership is disjoint and task
dependencies do not require sequential execution.

Use repo docs for durable work tracking. Use ignored local
`.worktrees/state/<worktree-slug>.md` records for active parallel Codex
processes.

`.worktrees/` is gitignored. Live state under `.worktrees/state/` must not be
committed. Repo docs and Git history are the durable record after work
completes.

## Subagent Trigger Gate

At task start, the coordinator must decide whether independent subagent work is
available.

Use `docs/automation/subagent-prompts.md` for compact review, implementation,
PR-readiness, and failure-investigation prompts.

Use subagents when all of these are true:

- there are 2+ independent review, verification, investigation, or implementation lanes
- target files are disjoint or the subagents are read-only
- the parent agent can continue useful coordination work while subagents run
- the user has explicitly authorized subagents for the task or the task is covered
  by standing authorization in the current session

Common default subagent lanes:

- tooling or Makefile review
- documentation consistency review
- git/worktree cleanup review
- focused test or CI failure investigation
- isolated implementation tasks with disjoint file ownership

For small doc-only or tooling-documentation changes, prefer one implementer and
one final read-only reviewer. Skip multi-reviewer loops unless the change
touches code behavior, contracts, runtime validation, or architecture decisions.

Do not wait for a reminder when these conditions are met. If authorization is
missing, ask once near the start instead of doing all work inline.

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
- requirement to update repo docs that describe state, plans, milestones, bugs,
  or status

## Local State Record

Create one ignored local state file per active worktree:
`.worktrees/state/<worktree-slug>.md`.

Use `docs/automation/state-record-template.md` for the required shape.

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
4. Mark the local state `Ready For PR` after validation passes.
5. Create PR when authorized.
6. Mark local state `PR Open` when tracking it.
7. After merge, the owning agent must remove its local worktree, then delete
   the matching `.worktrees/state/<worktree-slug>.md` file.

Parallel agents normally run in separate terminals with separate worktrees. Each
agent owns cleanup for the worktree it created; do not leave merged worktrees
behind unless cleanup is blocked, and report the blocker in the handoff.

## Context-Saving Pattern

The parent agent owns coordination, final integration, repo-doc updates, and
final handoff. Subagents own bounded investigation or implementation lanes and
return short handoffs only.

Subagent output should include:

- finding or change summary, capped at five bullets
- files inspected or changed
- validation command and outcome, plus a log path when available
- blockers or conflicts

Subagents should not paste full logs, full diffs, or broad repo scans unless the
parent explicitly asks for that evidence.

## Subagent Closeout

After a subagent result is integrated:

1. Record the relevant finding, changed files, validation, or blocker in the
   coordinator notes or handoff.
2. Close the completed subagent so stale threads do not consume tool slots or
   distract later coordination.
3. Reuse the result summary instead of re-reading the full subagent transcript.

Do not leave completed subagents open after their results are accepted, rejected,
or superseded.

## Token Budget Rules

Treat context as a finite execution resource.

- Prefer narrow prompts and bounded file ownership for each subagent.
- Ask subagents for short structured handoffs, not full logs.
- Use `git diff --name-only`, `git diff --stat`, focused `rg`, and targeted
  file reads before full diffs or broad scans.
- Summarize validation with command, outcome, and the smallest proof output.
- Write a checkpoint to `.worktrees/state/<worktree-slug>.md`, an issue, a PR,
  or a handoff before compaction risk becomes likely.
- Stop and hand off before forced compaction when the next safe action is
  resumable from durable state.

## Remote Coordination

Parallel agents may work on local files at the same time when ownership is
disjoint. Remote GitHub operations such as PR creation, review requests, labels,
or merges should still be serialized by the coordinator so the final handoff
stays coherent.

Prefer the GitHub plugin for structured repository, issue, PR, review, diff,
commit, CI, comment, label, and merge operations.
