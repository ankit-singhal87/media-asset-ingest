# TASK-5-3: Add Workflow Orchestrator Service

## Status

In Progress

## Linked Work

- GitHub issue: #107
- Milestone: MILESTONE-5
- User stories:
  - USER-STORY-9
  - USER-STORY-10
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #19
- Blocked by: none
- Blocks: #108, #109

## Ownership

Primary lane: Pulse

Supporting lanes:

- Forge
- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Workflow.Orchestrator`
- `src/MediaIngest.Workflow`
- `tests/MediaIngest.Workflow.Tests`
- `MediaIngest.slnx`
- `deploy/dapr`
- `deploy/k8s`
- `docker-compose.yml`
- `docs/architecture/002-workflow-orchestration.md`
- `docs/status/work-log.md`

## Investigation Targets

Read before editing:

- `docs/architecture/000-system-overview.md` - confirms Dapr Workflow, Azure Service Bus, PostgreSQL, and workflow UI boundaries.
- `docs/architecture/002-workflow-orchestration.md` - defines package workflow, child workflow, and Dapr/business state separation.
- `docs/adr/ADR-0003-use-dapr-workflow.md` - records the accepted code-first Dapr Workflow decision.
- `src/MediaIngest.Workflow` - current in-process workflow lifecycle and graph projection foundation.
- `src/MediaIngest.Api/IngestRuntimeService.cs` - current API-local runtime facade that later tasks will call through or replace.
- Dapr workflow architecture and .NET workflow docs - confirm workflow app registration and runtime-state responsibilities.

## BDD Scenario

```gherkin
Scenario: Orchestrator exposes the workflow bounded-context boundary
  Given the workflow orchestrator service starts
  When other backend components need to discover orchestrator-owned workflow code
  Then the orchestrator assembly exposes a stable public boundary marker
  And Dapr workflow runtime state remains separate from business PostgreSQL state
```

## TDD Expectations

RED:

```bash
make test-dotnet-workflow-summary
```

Expected before implementation: workflow tests fail because no orchestrator service boundary assembly exists.

GREEN:

```bash
make test-dotnet-workflow-summary
```

Expected after implementation: workflow tests pass with orchestrator service boundary coverage.

REFACTOR:

- Keep `make test-dotnet-workflow-summary` passing after cleanup.
- Run broader validation before PR readiness because this task changes service/runtime shape.

## Implementation Notes

- Create `MediaIngest.Workflow.Orchestrator` as a standalone bounded-context
  service boundary for workflow hosting.
- The service owns workflow definitions, instance orchestration entrypoints, and graph-projection endpoints or application services.
- Dapr provides durable workflow execution, state, timers, external events, and lifecycle management.
- Keep PostgreSQL business status, timeline, and audit state separate from Dapr runtime state.
- Start with package ingest, but keep naming and service boundaries generic enough for multiple workflow definitions.
- Stop and ask before adding or upgrading Dapr Workflow SDK dependencies.
- This task intentionally stops at the service boundary and public assembly
  marker. Real Dapr Workflow SDK registration is deferred to a later
  dependency-approved task.
- Do not provision Azure resources, create paid cloud assets, or introduce secrets.

## Validation

Minimal validation:

```bash
make test-dotnet-workflow-summary
git diff --check
```

Stronger validation before PR readiness:

```bash
make validate-summary
```

## Required Documentation Updates

- `docs/architecture/002-workflow-orchestration.md`
- `docs/status/work-log.md`
- `docs/status/current-status.md` when focus or next action changes
- `docs/product/backlog.md` when task status changes
- `docs/plans/task-index.md` only if this local task file is renamed or deleted

## Completion Checklist

- [ ] Scope and ownership lane confirmed.
- [ ] Investigation targets reviewed.
- [ ] Dependency changes approved before implementation.
- [ ] BDD scenario confirmed or updated.
- [ ] Failing test observed before production code, unless exception documented.
- [ ] Orchestrator service added within target files.
- [ ] Workflow boundary marker covered by tests.
- [ ] Dapr runtime state and PostgreSQL business state boundaries documented.
- [ ] Validation command run and evidence recorded.
- [ ] Required docs updated.
- [ ] Handoff result prepared.

## Handoff Result

```text
status: completed | blocked | escalation-needed
taskId: TASK-5-3
github:
  issues:
    - #107
stories:
  - USER-STORY-9
  - USER-STORY-10
filesChanged:
  - src/MediaIngest.Workflow.Orchestrator
  - src/MediaIngest.Workflow
  - tests/MediaIngest.Workflow.Tests
validation:
  - command: make test-dotnet-workflow-summary
    outcome: pass/fail/not-run
  - command: git diff --check
    outcome: pass/fail/not-run
docsUpdated:
  - docs/architecture/002-workflow-orchestration.md
  - docs/status/work-log.md
blockers:
  - none or details
next:
  - Proceed to TASK-5-4 attribute-discovered workflow definition catalog.
```
