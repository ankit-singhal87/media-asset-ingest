# TASK-5-1: Add Dapr Workflow Skeleton

## Status

Planned

## Linked Work

- GitHub issue: #31
- Milestone: MILESTONE-5
- User stories:
  - USER-STORY-9
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #19
- Blocked by: TASK-2-1 and TASK-2-2
- Blocks: nested workflow and reconciliation tasks.

## Ownership

Primary lane: Pulse

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Workflow`
- `tests/MediaIngest.Workflow.Tests`
- `deploy/dapr`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`

## BDD Scenario

```gherkin
Scenario: Package workflow starts from a package ingest request
  Given a package has been detected and accepted for work
  When orchestration starts
  Then the package workflow records a workflow instance and prepares child work
```

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```
