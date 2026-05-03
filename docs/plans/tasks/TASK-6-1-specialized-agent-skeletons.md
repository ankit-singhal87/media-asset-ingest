# TASK-6-1: Add Specialized Agent Worker Skeletons

## Status

Planned

## Linked Work

- GitHub issue: #34
- Milestone: MILESTONE-6
- User stories:
  - USER-STORY-7
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #17
- Blocked by: TASK-2-1 and TASK-2-2
- Blocks: specialized media processing tasks.

## Ownership

Primary lane: Essence

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Agents.Video`
- `src/MediaIngest.Agents.Audio`
- `src/MediaIngest.Agents.Text`
- `src/MediaIngest.Agents.Other`
- `tests/MediaIngest.Agents.*.Tests`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`

## BDD Scenario

```gherkin
Scenario: Specialized agents own separate work categories
  Given classified media work items
  When work is routed to specialized agents
  Then each agent project owns only its assigned media category
```

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```
