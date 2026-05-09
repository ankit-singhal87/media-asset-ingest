# Agent Handoff

Every implementation, review, or bug-fix agent should return a concise handoff.
Use repo-relative paths in handoffs. Do not include workstation-specific
absolute paths unless reporting an environment-specific failure where the exact
path is evidence.

## Required Handoff Format

Keep handoffs compact. Use at most five finding bullets, summarize validation as
command plus pass/fail and log path, avoid quoted file contents, and include logs
only when a failure cannot be understood from the summary.

```text
status: completed | blocked | escalation-needed | no-op
scope:
  taskId: TASK-<milestone>-<number> | none
  stories:
    - USER-STORY-<number>
  bugs:
    - BUG-<number>
ownership:
  primaryLane: <lane>
  supportingLanes:
    - <lane>
workflow:
  superpowers:
    skillsUsed:
      - <skill name or none>
    subagentsUsed: yes | no
    reason: <why subagents were used or skipped>
  context:
    checkpointWritten: yes | no
    nextResumePoint: <short summary or not-applicable>
filesChanged:
  - <path>
docsUpdated:
  - <path>
validation:
  - command: <command>
    outcome: passed | failed | not-run
    notes: <short notes>
github:
  project:
    validated: yes | no | not-applicable
    evidence: <command/output summary>
  issues:
    - <issue/pr URL or none>
tdd:
  red: <command/evidence or exception>
  green: <command/evidence or not-applicable>
blockers:
  - <none or details>
next:
  - <recommended next action>
pr:
  authorized: yes | no
  url: <url or none>
  state: not-created | open | merged | blocked
cleanup:
  worktreePath: <path or not-applicable>
  stateRecord: <path or not-applicable>
  targetBranchFastForwarded: yes | no | not-applicable
  worktreeRemoved: yes | no | not-applicable
  localBranchDeleted: yes | no | not-applicable
  remoteBranchDeleted: yes | no | not-applicable
  notes: <short notes>
```

## Escalation Reasons

Use `status: escalation-needed` when:

- target files are missing or stale
- a task needs files outside its allowed list
- dependency installation is required
- a cloud, secret, or paid action is required
- architecture or contract decisions conflict
- validation fails for an unclear reason
- another agent is working in the same file scope
- context is approaching the agreed budget and no durable checkpoint exists
- compaction risk is high and the next safe action is to write a handoff
- subagent authorization is missing for clearly independent work

## PR Handoff

If automatic PR creation is authorized and completed, include the PR URL. If it
is not authorized, set `pr.authorized: no` and report the branch as ready for PR
only after validation passes.
