# TASK-6-1: Add Command Execution Foundation

## Status

Completed

## Linked Work

- GitHub issue: #34
- Milestone: MILESTONE-6
- User stories:
  - USER-STORY-7
- Bugs:
  - None

This task originally created media-specific agent skeletons. That direction was
superseded by generic command-routing contracts and light, medium, or heavy
execution classes.

## Ownership

Primary lane: Essence

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Contracts/Commands`
- `tests/MediaIngest.Contracts.Tests`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`

## BDD Scenario

```gherkin
Scenario: Media commands route to execution capacity
  Given media commands with file size metadata
  When command routing policy selects execution capacity
  Then each command keeps a semantic topic and receives light, medium, or heavy execution class
```

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```
