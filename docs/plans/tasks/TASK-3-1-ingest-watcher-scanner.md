# TASK-3-1: Add Ingest Watcher And Scanner Foundation

## Status

Planned

## Linked Work

- GitHub issue: #35
- Milestone: MILESTONE-3
- User stories:
  - USER-STORY-1
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #11
- Blocked by: TASK-2-1 and TASK-2-2
- Blocks: later manifest and file enumeration tasks.

## Ownership

Primary lane: Mount

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Worker.Watcher`
- `tests/MediaIngest.Worker.Watcher.Tests`
- `src/MediaIngest.Contracts`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`

## BDD Scenario

```gherkin
Scenario: Watcher identifies package directories under the ingest mount
  Given a configured ingest mount path
  When immediate child directories exist under that path
  Then the watcher treats each child directory as an ingest package candidate
```

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```
