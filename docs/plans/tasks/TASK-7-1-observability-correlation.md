# TASK-7-1: Add Observability Correlation Foundation

## Status

Completed

## Linked Work

- GitHub issue: #37
- Milestone: MILESTONE-7
- User stories:
  - USER-STORY-11
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #21
- Blocked by: None
- Blocks: cross-service progress logging tasks.

## Ownership

Primary lane: Beacon

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Observability`
- `tests/MediaIngest.Observability.Tests`
- `docs/architecture/004-observability-and-ui.md`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`

## BDD Scenario

```gherkin
Scenario: Services share correlation identifiers
  Given ingest work crosses watcher, workflow, agents, and UI
  When logs and timeline events are emitted
  Then package, workflow, agent, and work item identifiers are consistently named
```

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```
