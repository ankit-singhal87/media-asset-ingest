# TASK-8-5: Generate Workflow Graph DTOs From Orchestrator Definitions

## Status

In Progress

## Linked Work

- GitHub issue: #109
- Milestone: MILESTONE-8
- User stories:
  - USER-STORY-12
  - USER-STORY-14
  - USER-STORY-15
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #22
- Blocked by: #107, #108
- Blocks: #110

## Ownership

Primary lane: Canvas

Supporting lanes:

- Pulse
- Gauge
- Beacon

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Workflow.Orchestrator`
- `src/MediaIngest.Workflow`
- `src/MediaIngest.Api`
- `src/MediaIngest.Contracts/Workflow`
- `tests/MediaIngest.Workflow.Tests`
- `tests/MediaIngest.Api.Tests`
- `web/ingest-control-plane`
- `docs/architecture/004-observability-and-ui.md`
- `docs/status/work-log.md`

## Investigation Targets

Read before editing:

- `src/MediaIngest.Contracts/Workflow/WorkflowGraphDto.cs` - UI graph contract that must remain stable.
- `src/MediaIngest.Contracts/Workflow/WorkflowNodeDto.cs` - node fields for child workflow and work-item correlation.
- `src/MediaIngest.Workflow/PackageWorkflowGraphProjection.cs` - current graph projection that should be replaced or narrowed.
- `src/MediaIngest.Api/IngestRuntimeService.cs` - current API-local graph construction and runtime facade.
- `web/ingest-control-plane/src/workflowGraph.ts` - current UI graph fetch and rendering model.
- `docs/architecture/004-observability-and-ui.md` - UI projection, drilldown, and node-detail expectations.

## BDD Scenario

```gherkin
Scenario: API returns orchestrator-generated workflow graph
  Given a package workflow instance exists
  And the orchestrator catalog contains the package ingest workflow definition
  When the UI facade requests the workflow graph
  Then the API returns a WorkflowGraphDto generated from orchestrator-owned definition metadata
  And API-local topology construction is not used
```

## TDD Expectations

RED:

```bash
make test-dotnet-api-summary
```

Expected before implementation: API tests fail because the graph endpoint does not call an orchestrator-backed graph projection.

GREEN:

```bash
make test-dotnet-api-summary
make test-dotnet-workflow-summary
```

Expected after implementation: API and workflow graph tests pass with orchestrator-generated definition and instance graphs.

REFACTOR:

- Keep `WorkflowGraphDto` as the UI contract.
- Keep unknown workflow instances returning not found.
- Keep node details backed by existing timeline and log DTO paths.

## Implementation Notes

- Generate definition graphs from orchestrator-owned workflow definition metadata.
- Generate instance graphs from the same topology overlaid with runtime status from Dapr workflow state and business progress projections.
- Attach dynamic command work-item nodes at descriptor-defined command dispatch or wait sections.
- Keep the control-plane API as the UI facade: `GET /api/workflows/{workflowInstanceId}/graph` continues to be the UI entrypoint.
- The API should call the orchestrator service and forward or lightly overlay `WorkflowGraphDto`.
- Remove or retire API-local hard-coded graph generation from `IngestRuntimeService`.
- Preserve child workflow drilldown identifiers already represented by `childWorkflowInstanceId`.
- In the local foundation, the overlay uses business package status and
  persisted command outbox envelopes while real Dapr workflow runtime status
  remains deferred with SDK hosting.

## Validation

Minimal validation:

```bash
make test-dotnet-api-summary
make test-dotnet-workflow-summary
make test-dotnet-contracts
git diff --check
```

Stronger validation before PR readiness:

```bash
make validate-summary
```

## Required Documentation Updates

- `docs/architecture/004-observability-and-ui.md`
- `docs/status/work-log.md`
- `docs/status/current-status.md` when focus or next action changes
- `docs/product/backlog.md` when task status changes
- `docs/plans/task-index.md` only if this local task file is renamed or deleted

## Completion Checklist

- [ ] Scope and ownership lane confirmed.
- [ ] Investigation targets reviewed.
- [ ] BDD scenario confirmed or updated.
- [ ] Failing API graph test observed before production code, unless exception documented.
- [ ] Orchestrator exposes definition graph projection.
- [ ] Orchestrator exposes instance graph projection.
- [ ] API graph endpoint uses orchestrator-backed projection.
- [ ] API-local topology construction removed or made non-authoritative.
- [ ] Unknown workflow instance still returns not found.
- [ ] Validation command run and evidence recorded.
- [ ] Required docs updated.
- [ ] Handoff result prepared.

## Handoff Result

```text
status: completed | blocked | escalation-needed
taskId: TASK-8-5
github:
  issues:
    - #109
stories:
  - USER-STORY-12
  - USER-STORY-14
  - USER-STORY-15
filesChanged:
  - src/MediaIngest.Workflow.Orchestrator
  - src/MediaIngest.Workflow
  - src/MediaIngest.Api
  - tests/MediaIngest.Api.Tests
  - tests/MediaIngest.Workflow.Tests
validation:
  - command: make test-dotnet-api-summary
    outcome: pass/fail/not-run
  - command: make test-dotnet-workflow-summary
    outcome: pass/fail/not-run
  - command: git diff --check
    outcome: pass/fail/not-run
docsUpdated:
  - docs/architecture/004-observability-and-ui.md
  - docs/status/work-log.md
blockers:
  - none or details
next:
  - Proceed to TASK-8-6 UI rendering for waits and command dependencies.
```
