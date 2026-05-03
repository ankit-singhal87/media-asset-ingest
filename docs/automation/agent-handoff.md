# Agent Handoff

Every implementation, review, or bug-fix agent should return a concise handoff.

## Required Handoff Format

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

## PR Handoff

If automatic PR creation is authorized and completed, include the PR URL. If it
is not authorized, set `pr.authorized: no` and report the branch as ready for PR
only after validation passes.
