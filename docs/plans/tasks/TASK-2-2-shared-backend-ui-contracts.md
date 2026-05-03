# TASK-2-2: Define Shared Backend And UI Contracts

## Status

Completed

## Linked Work

- GitHub issue: #32
- Milestone: MILESTONE-2
- User stories:
  - USER-STORY-16
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #26
- Blocked by: None
- Blocks: TASK-5-1, TASK-8-1, and contract-consuming backend tasks.

## Ownership

Primary lane: Forge

Supporting lanes:

- Atlas
- Canvas
- Pulse

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Contracts`
- `tests/MediaIngest.Contracts.Tests`
- `MediaIngest.sln`
- `scripts/dev/test-dotnet.sh`
- `docs/architecture`
- `docs/plans/tasks/TASK-2-2-shared-backend-ui-contracts.md`
- `docs/plans/task-index.md`
- `docs/status/current-status.md`
- `docs/status/work-log.md`

## Investigation Targets

Read before editing:

- GitHub issue #32 and its parent/sub-issue/dependency relationships.
- `docs/architecture/000-system-overview.md` for core flow.
- `docs/architecture/002-workflow-orchestration.md` for graph semantics.
- `docs/product/user-stories.md` for workflow UI and backend acceptance themes.

## BDD Scenario

```gherkin
Scenario: Backend and UI agents share stable workflow graph names
  Given backend, workflow, and UI tasks run in separate worktrees
  When they need workflow graph and node log data
  Then they use the same named contract types and field meanings
```

## TDD Expectations

RED:

```bash
make test-dotnet
```

Expected before implementation: contract tests fail because shared contract
types are missing.

GREEN:

```bash
make test-dotnet
make validate
git diff --check
```

Expected after implementation: contract tests and repository validation pass.

## Implementation Notes

- Keep contracts minimal and explicit.
- Define names and shapes needed by the first Dapr workflow and mocked React UI
  slices.
- Do not implement persistence, message broker clients, Dapr runtime behavior,
  or UI rendering in this task.

## Validation

Minimal validation:

```bash
make test-dotnet
make validate
git diff --check
```

GitHub tracker validation:

```bash
make github-project-summary
make github-project-hierarchy
```

## Required Documentation Updates

- `docs/plans/task-index.md`
- `docs/status/current-status.md`
- `docs/status/work-log.md`
- Relevant architecture docs when contract semantics are added.
