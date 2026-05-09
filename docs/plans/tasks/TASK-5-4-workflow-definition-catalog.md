# TASK-5-4: Add Attribute-Discovered Workflow Definition Catalog

## Status

In Progress

## Linked Work

- GitHub issue: #108
- Milestone: MILESTONE-5
- User stories:
  - USER-STORY-9
  - USER-STORY-10
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #19
- Blocked by: #107
- Blocks: #109, #110

## Ownership

Primary lane: Pulse

Supporting lanes:

- Beacon
- Canvas

## Target Files

Agents may edit only these files unless they escalate:

- `src/MediaIngest.Workflow.Orchestrator`
- `src/MediaIngest.Contracts/Workflow/WorkflowNodeKind.cs`
- `tests/MediaIngest.Workflow.Tests`
- `tests/MediaIngest.Contracts.Tests`
- `docs/architecture/002-workflow-orchestration.md`
- `docs/architecture/004-observability-and-ui.md`
- `docs/status/work-log.md`

## Investigation Targets

Read before editing:

- `docs/architecture/002-workflow-orchestration.md` - workflow, child workflow, and state-boundary rules.
- `docs/architecture/004-observability-and-ui.md` - graph node, status, drilldown, and node-detail projection expectations.
- `src/MediaIngest.Workflow/PackageWorkflowGraphProjection.cs` - current static package graph projection behavior.
- `src/MediaIngest.Workflow/PreparedChildWork.cs` - current stable child workflow ID contract.
- `src/MediaIngest.Contracts/Workflow` - shared UI graph DTO contract that later projection tasks must preserve.

## BDD Scenario

```gherkin
Scenario: Startup discovers package ingest workflow topology
  Given workflow and activity code is annotated with topology metadata
  When the orchestrator builds the workflow definition catalog at startup
  Then the package ingest workflow definition contains stable nodes, edges, child workflows, waits, and command dispatch points
  And duplicate or missing node identifiers fail startup validation
```

## TDD Expectations

RED:

```bash
make test-dotnet-workflow-summary
```

Expected before implementation: catalog discovery tests fail because topology attributes and catalog validation do not exist.

GREEN:

```bash
make test-dotnet-workflow-summary
```

Expected after implementation: workflow catalog tests pass for discovery, stable package graph metadata, child workflow IDs, and validation failures.

REFACTOR:

- Keep `make test-dotnet-workflow-summary` passing after cleanup.
- Keep topology metadata readable from class or method annotations without method-body analysis.

## Implementation Notes

- Add attribute-based compile-time topology metadata for workflow, activity, wait, command dispatch, and child-workflow nodes.
- Capture stable node IDs, display names, node kinds, ordering or dependency edges, wait points, command dispatch points, and child workflow references.
- Use startup reflection to scan known orchestrator workflow assemblies into an in-memory workflow definition catalog.
- Fail fast on missing required node IDs, duplicate node IDs within a workflow definition, duplicate definition IDs or versions, and edges that reference unknown nodes.
- Avoid workflow method-body analysis in this slice.
- Keep the definition source as compiled code plus attributes, not DB-authored workflows.
- Keep the first definition package ingest, but do not hard-code catalog internals to package-only concepts.
- Extend the shared `WorkflowNodeKind` contract with `Wait`,
  `CommandDispatch`, `CommandCompletion`, and `Finalization` so downstream API
  and UI graph projections can render orchestrator-defined waits and command
  dependency points without private enum mappings.

## Validation

Minimal validation:

```bash
make test-dotnet-workflow-summary
make test-dotnet-contracts
git diff --check
```

Stronger validation before PR readiness:

```bash
make validate-summary
```

## Required Documentation Updates

- `docs/architecture/002-workflow-orchestration.md`
- `docs/architecture/004-observability-and-ui.md`
- `docs/status/work-log.md`
- `docs/status/current-status.md` when focus or next action changes
- `docs/product/backlog.md` when task status changes
- `docs/plans/task-index.md` only if this local task file is renamed or deleted

## Completion Checklist

- [ ] Scope and ownership lane confirmed.
- [ ] Investigation targets reviewed.
- [ ] BDD scenario confirmed or updated.
- [ ] Failing catalog discovery test observed before production code, unless exception documented.
- [ ] Topology attributes added.
- [ ] Startup reflection discovery added.
- [ ] Catalog validation covers duplicate and missing IDs.
- [ ] Package ingest definition contains stable nodes, edges, child workflow references, waits, and command dispatch points.
- [ ] Validation command run and evidence recorded.
- [ ] Required docs updated.
- [ ] Handoff result prepared.

## Handoff Result

```text
status: completed | blocked | escalation-needed
taskId: TASK-5-4
github:
  issues:
    - #108
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
  - docs/architecture/004-observability-and-ui.md
  - docs/status/work-log.md
blockers:
  - none or details
next:
  - Proceed to TASK-8-5 orchestrator-backed WorkflowGraphDto projection.
```
