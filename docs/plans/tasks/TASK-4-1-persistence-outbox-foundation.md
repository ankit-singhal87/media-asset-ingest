# TASK-4-1: Add Persistence And Outbox Foundation

## Status

Planned

## Linked Work

- GitHub issue: #36
- Milestone: MILESTONE-4
- User stories:
  - USER-STORY-8
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #18
- Blocked by: TASK-2-1 and TASK-2-2
- Blocks: Azure Service Bus routing tasks.

## Ownership

Primary lane: Vault

Supporting lanes:

- Courier
- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Persistence`
- `src/MediaIngest.Worker.Outbox`
- `tests/MediaIngest.Persistence.Tests`
- `tests/MediaIngest.Worker.Outbox.Tests`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`

## BDD Scenario

```gherkin
Scenario: Business state and outbox writes share one persistence boundary
  Given package work creates business state and outbound commands
  When the persistence layer saves the work
  Then business state and outbox records are handled as one durable unit
```

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```
