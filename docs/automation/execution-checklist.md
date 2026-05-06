# Execution Checklist

Use this checklist before an agent starts and before it reports completion.

## Change Mode Matrix

Use the lightest mode that preserves traceability and isolation.

| Mode | Worktree | Issue ID | Branch | Staging | Commit/Push/PR |
| --- | --- | --- | --- | --- | --- |
| Answer or review only | no | no | no | no | no |
| Tiny doc hygiene | optional when checkout is clean | optional | optional | when making a local commit | local commit allowed; push/PR only when explicitly asked |
| Tooling or docs behavior | yes for PR-bound work | story/task preferred | yes for PR-bound work | before each local commit | local commits allowed; push/PR only when explicitly asked |
| Feature or bug implementation | yes | lowest available task, bug, or story | yes | before each local commit | local commits allowed; push/PR only when explicitly asked |
| Parallel or subagent implementation | yes | required when available | yes | coordinator decides before integration | local commits allowed; push/PR only when explicitly asked |
| Post-merge cleanup | existing checkout is acceptable | PR reference | no new branch | no | no new PR unless explicitly asked |

Local commits are allowed after relevant validation and should be small enough
to review or revert independently. Prefer committing each completed task or
coherent checkpoint instead of accumulating one large final commit.

Push and PR creation require explicit user authorization or explicit task/plan
authorization after validation. Completing implementation, staging a diff, or
making a local commit does not imply permission to push or open a PR.

Staging is optional until local commit or PR readiness unless the user asks for
staged changes. When staging is used, keep staged paths limited to the intended
scope and report staged and unstaged files separately.

## Before Starting

- [ ] Read `AGENTS.md`.
- [ ] Read `docs/automation/README.md`.
- [ ] Select the change mode from the matrix above.
- [ ] Decide whether worktree, branch, and GitHub issue linkage are required for that mode.
- [ ] Identify applicable Superpowers skills or explicitly state none apply.
- [ ] Decide whether the task has 2+ independent lanes suitable for subagents.
- [ ] If subagents are suitable and authorized, dispatch them before deep local work.
- [ ] Set a context budget: plan a handoff/checkpoint before long logs, broad diffs, or compaction risk.
- [ ] Read `docs/automation/github-projects.md`.
- [ ] Identify ownership lane from `docs/automation/roles.md`.
- [ ] Identify linked GitHub issue, user stories, and milestone.
- [ ] Check the linked GitHub issue when remote context is needed.
- [ ] Identify target files.
- [ ] Identify validation command.
- [ ] Check `.worktrees/state/*.md` and `git worktree list` for overlapping work.
- [ ] Confirm no other parallel task owns the same target files.
- [ ] Create or update the current task capsule in `.worktrees/state/<worktree-slug>.md` with goal, branch/worktree, changed files, validation status, next action, and blockers.

## During Work

- [ ] Keep edits inside target files.
- [ ] Escalate before dependency, cloud, secret, or out-of-scope work.
- [ ] Write or update a durable checkpoint before long logs, broad diffs, or compaction risk.
- [ ] Inspect diffs in this order by default: `git diff --name-only`, `git diff --stat`, targeted `git diff -- <file>`, then full diff only when needed.
- [ ] Before `make validate`, `local-runtime-smoke`, Docker validation, or other long commands, update the state record, then run a summary validation command when available.
- [ ] Close completed subagents after their findings are integrated.
- [ ] Follow BDD/TDD for behavior changes.
- [ ] Update GitHub issue or simple Project status only when tracker state changes.
- [ ] Update task, story, milestone, status, and bug docs when durable repo context changes.
- [ ] Record validation evidence.

## Before Reporting Completion

- [ ] Run the cheapest relevant validation.
- [ ] Prefer `*-summary` validation targets for broad commands, and report the log path.
- [ ] Run stronger validation if the change touches runtime, contracts, infra, or tooling.
- [ ] Run `git diff --check`.
- [ ] Run lightweight GitHub tracker validation when tracker state changed.
- [ ] Update required docs.
- [ ] Review staged and unstaged paths separately.
- [ ] Prefer small local commits after validation when the change is ready.
- [ ] Confirm push and PR creation are authorized before running them.
- [ ] If reporting PR readiness, confirm `docs/automation/pr-checklist.md` is satisfied.
- [ ] If a PR was merged, fast-forward the target branch locally when safe.
- [ ] If a PR was merged, remove completed local worktrees after confirming no uncommitted work remains.
- [ ] If a PR was merged, delete completed local feature branches when no longer needed.
- [ ] If a PR was merged, delete completed `.worktrees/state/<worktree-slug>.md` records.
- [ ] Prepare handoff using `docs/automation/agent-handoff.md`.
- [ ] If PR creation is authorized, complete `docs/automation/pr-checklist.md`.
- [ ] Report any validation not run and why.
