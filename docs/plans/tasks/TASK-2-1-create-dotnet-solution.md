# TASK-2-1: Create .NET Solution Skeleton

## Status

Completed

## Linked Work

- GitHub issue: #30
- Milestone: MILESTONE-2
- User stories:
  - USER-STORY-16
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #26
- Blocked by: none
- Blocks: #32 and later implementation tasks.

## Ownership

Primary lane: Forge

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `MediaIngest.slnx`
- `src/Directory.Build.props`
- `src/MediaIngest.*`
- `tests/MediaIngest.*`
- `Makefile`
- `package.json`
- `scripts/dev`
- `docs/automation/commands.md`
- `docs/automation/validation.md`
- `docs/standards/tooling.md`
- `docs/status/current-status.md`
- `docs/status/work-log.md`
- `docs/plans/task-index.md`

## Investigation Targets

Read before editing:

- GitHub issue #30 and its parent/sub-issue/dependency relationships.
- `docs/automation/repo-map.md` for planned project names.
- `docs/standards/tooling.md` for Make/npm command expectations.

## BDD Scenario

```gherkin
Scenario: Developer validates the empty .NET foundation
  Given the repository has no buildable .NET solution
  When the solution skeleton task is completed
  Then a developer can run the canonical .NET validation command successfully
```

## TDD Expectations

RED:

```bash
make test-dotnet
```

Expected before implementation: command or solution lookup fails because the
.NET solution target does not exist.

GREEN:

```bash
make test-dotnet
make validate
git diff --check
```

Expected after implementation: commands pass locally.

## Implementation Notes

- Create the smallest buildable solution and test project structure.
- Do not implement ingest, workflow, persistence, agent, or UI behavior.
- Prefer command names that can remain stable after runtime projects are added.

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```

## Required Documentation Updates

- `docs/plans/task-index.md`
- `docs/status/current-status.md`
- `docs/status/work-log.md`
- `docs/automation/commands.md`
- `docs/automation/validation.md`
- `docs/standards/tooling.md`
