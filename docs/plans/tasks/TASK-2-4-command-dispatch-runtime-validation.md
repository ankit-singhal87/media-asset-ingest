# TASK-2-4: Deepen Docker-First Runtime Validation For Command Dispatch

## Status

Completed

## Linked Work

- GitHub issue: #105
- Milestone: MILESTONE-2
- User stories:
  - USER-STORY-16
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #3
- Blocked by: none
- Blocks: none

## Ownership

Primary lane: Forge

Supporting lanes:

- Gauge
- Courier
- Essence

## Target Files

- `deploy/docker`
- `scripts/dev`
- `Makefile`
- `README.md`
- `docs/automation`
- `docs/status/current-status.md`
- `docs/status/work-log.md`
- `docs/plans/task-index.md`

## BDD Scenario

```gherkin
Scenario: Docker runtime smoke proves local command dispatch boundaries
  Given the local Compose runtime is started
  And the API, UI, PostgreSQL, outbox worker, and command-runner containers are running
  When the runtime smoke ingests a manifest package with command-routed files
  Then the smoke verifies workflow command-node evidence
  And PostgreSQL contains dispatched command outbox rows grouped by execution class
  And each local command-runner host exposes its configured execution class
```

## TDD Evidence

RED:

```bash
sh scripts/dev/test-local-compose-check.sh
```

Observed before implementation: the dry-run plan test failed because the
runtime smoke plan did not yet describe command-runner boundary or grouped
execution-class evidence.

GREEN:

```bash
sh scripts/dev/test-local-compose-check.sh
```

Observed after implementation:

```text
Local Compose check script tests passed.
```

## Implementation Notes

- The Docker-first smoke remains local-only and does not create Azure resources,
  read secrets, push images, or require paid cloud validation.
- Command-runner evidence is boundary-level in this slice: service state,
  startup log configuration, and dispatched outbox routing metadata.
- Real Azure Service Bus consumption and media command execution remain
  deferred.

## Validation

Minimal validation:

```bash
sh scripts/dev/test-local-compose-check.sh
sh scripts/dev/local-compose-check.sh --runtime-smoke --dry-run
make validate-automation-summary
```

Required runtime validation:

```bash
make local-runtime-smoke
```

Broad validation:

```bash
make validate-summary
git diff --check
```
