# TASK-3-2: Integrated Local Ingest Demo

## Status

Completed

## Linked Work

- GitHub issue: none
- Milestone: MILESTONE-3, MILESTONE-4, MILESTONE-5, MILESTONE-8
- User stories:
  - USER-STORY-1
  - USER-STORY-3
  - USER-STORY-4
  - USER-STORY-6
  - USER-STORY-8
  - USER-STORY-9
  - USER-STORY-11
  - USER-STORY-12
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: none
- Blocked by: none
- Blocks: none

## Ownership

Primary lane: Gauge

Supporting lanes:

- Mount
- Essence
- Courier
- Pulse
- Beacon
- Canvas

## Target Files

- `src/MediaIngest.Api`
- `tests/MediaIngest.Api.Tests/Program.cs`
- `scripts/dev/local-e2e-smoke.sh`
- `README.md`
- `docs/automation/commands.md`
- `docs/automation/validation.md`
- `docs/plans/task-index.md`
- `docs/status/current-status.md`
- `docs/status/work-log.md`
- `docs/product/backlog.md`

## BDD Scenario

```gherkin
Scenario: Local ingest projects discovered files into routed command work
  Given the local ingest watcher is running
  And a ready package contains manifest files and media, audio, text, and other files
  When the package is accepted
  Then the runtime records package state
  And every non-metadata file becomes a routed command outbox message
  And the workflow graph exposes scan, classification, dispatch, command, reconcile, and finalize nodes
```

## TDD Evidence

RED:

```bash
make test-dotnet-api
```

Observed failure before implementation:

```text
graph node count: expected '10', got '4'
```

GREEN:

```bash
make test-dotnet-api
```

Observed after implementation:

```text
MediaIngest API smoke tests passed.
```

## Implementation Notes

- Local runtime remains in-process and in-memory.
- The local outbox publisher copies manifest files and treats generated media command envelopes as locally accepted no-op publications.
- Real Dapr Workflow, PostgreSQL, Azure Service Bus, and command runner execution remain deferred.

## Validation

Minimal validation:

```bash
make test-dotnet-api
sh -n scripts/dev/local-e2e-smoke.sh
sh scripts/dev/local-e2e-smoke.sh --dry-run
```

Stronger validation:

```bash
make test-dotnet
make validate
git diff --check
```

GitHub tracker validation:

Not run; this task has no GitHub issue or Project item.

## Completion Checklist

- [x] Scope and ownership lane confirmed.
- [x] GitHub issue and Project item checked or documented as absent.
- [x] Parent/sub-issue/dependency relationships checked or documented as absent.
- [x] Investigation targets reviewed.
- [x] BDD scenario confirmed.
- [x] Failing test observed before production code.
- [x] Implementation completed within target files.
- [x] Validation command run and evidence recorded.
- [x] Required docs updated.
- [x] GitHub Projects unchanged because no tracker state changed.
- [x] Handoff result prepared.

## Handoff Result

```text
status: completed
taskId: TASK-3-2
github:
  issues: []
  projectValidation:
    - not run; no tracker state changed
stories:
  - USER-STORY-1
  - USER-STORY-3
  - USER-STORY-4
  - USER-STORY-6
  - USER-STORY-8
  - USER-STORY-9
  - USER-STORY-11
  - USER-STORY-12
filesChanged:
  - src/MediaIngest.Api
  - tests/MediaIngest.Api.Tests/Program.cs
  - scripts/dev/local-e2e-smoke.sh
  - README.md
  - docs/automation/commands.md
  - docs/automation/validation.md
  - docs/plans/task-index.md
  - docs/status/current-status.md
  - docs/status/work-log.md
  - docs/product/backlog.md
validation:
  - command: make test-dotnet-api
    outcome: pass
  - command: sh -n scripts/dev/local-e2e-smoke.sh
    outcome: pass
  - command: sh scripts/dev/local-e2e-smoke.sh --dry-run
    outcome: pass
docsUpdated:
  - README.md
  - docs/automation/commands.md
  - docs/automation/validation.md
  - docs/plans/task-index.md
  - docs/plans/tasks/TASK-3-2-integrated-local-ingest-demo.md
  - docs/status/current-status.md
  - docs/status/work-log.md
  - docs/product/backlog.md
blockers:
  - none
next:
  - Run full repository validation before PR creation or merge.
```
