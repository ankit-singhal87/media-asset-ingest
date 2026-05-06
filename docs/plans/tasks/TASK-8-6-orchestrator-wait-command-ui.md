# TASK-8-6: Show Orchestrator Wait States And Command Dependencies In UI

## Status

Planned

## Linked Work

- GitHub issue: #110
- Milestone: MILESTONE-8
- User stories:
  - USER-STORY-12
  - USER-STORY-13
  - USER-STORY-14
  - USER-STORY-15
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #22
- Blocked by: #108, #109
- Blocks: none

## Ownership

Primary lane: Canvas

Supporting lanes:

- Pulse
- Beacon

## Target Files

Agents may edit only these files unless they escalate:

- `web/ingest-control-plane`
- `src/MediaIngest.Contracts/Workflow`
- `src/MediaIngest.Api`
- `tests/MediaIngest.Api.Tests`
- `docs/architecture/004-observability-and-ui.md`
- `docs/status/work-log.md`

## Investigation Targets

Read before editing:

- `web/ingest-control-plane/src/workflowGraph.ts` - Mermaid graph rendering and UI projection code.
- `web/ingest-control-plane/src/workflowGraph.test.ts` - focused UI graph tests.
- `src/MediaIngest.Contracts/Workflow/WorkflowNodeKind.cs` - shared node-kind contract.
- `src/MediaIngest.Contracts/Workflow/WorkflowNodeStatus.cs` - shared node-status contract.
- `src/MediaIngest.Contracts/Workflow/WorkflowNodeDetailsDto.cs` - node detail contract for timeline and log drilldown.
- `docs/architecture/004-observability-and-ui.md` - graph rendering, drilldown, back navigation, and node-detail expectations.

## BDD Scenario

```gherkin
Scenario: Operator sees waits and command dependencies in the workflow graph
  Given an orchestrator-generated package workflow graph contains activity, child workflow, wait, command dispatch, command completion, and finalization nodes
  When the React control plane renders the graph
  Then the Mermaid diagram displays those nodes and dependency edges
  And selecting a node still shows timeline and log details
  And child workflow drilldown and back navigation still work
```

## TDD Expectations

RED:

```bash
make test-ui
```

Expected before implementation: UI tests fail because wait nodes, command dependency nodes, or descriptor-driven graph nodes are not rendered or asserted.

GREEN:

```bash
make test-ui
```

Expected after implementation: UI tests pass for wait nodes, child workflow nodes, command nodes, drilldown, node details, and back navigation.

REFACTOR:

- Keep the Mermaid diagram as the single workflow graph visualization.
- Keep UI rendering driven by `WorkflowGraphDto`; do not add UI-local topology reconstruction.

## Implementation Notes

- Render activity, child workflow, wait, command dispatch, command completion, and finalization nodes from orchestrator-generated graph DTOs.
- Use existing shared node status colors and accessible node activation behavior.
- Preserve timeline and log detail panes for selected nodes.
- Preserve child workflow drilldown and parent back navigation.
- Add contract changes only if existing `WorkflowNodeKind` or `WorkflowNodeStatus` cannot express waits or command dependency sections; keep changes backward-compatible for existing graph DTOs.
- Do not add SignalR or live push in this slice; polling remains sufficient.

## Validation

Minimal validation:

```bash
make test-ui
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
- [ ] Failing UI test observed before production code, unless exception documented.
- [ ] Mermaid renders wait nodes.
- [ ] Mermaid renders child workflow nodes.
- [ ] Mermaid renders command dispatch and command completion dependency nodes.
- [ ] Node details still show timeline and log DTO data.
- [ ] Child workflow drilldown and back navigation still work.
- [ ] Validation command run and evidence recorded.
- [ ] Required docs updated.
- [ ] Handoff result prepared.

## Handoff Result

```text
status: completed | blocked | escalation-needed
taskId: TASK-8-6
github:
  issues:
    - #110
stories:
  - USER-STORY-12
  - USER-STORY-13
  - USER-STORY-14
  - USER-STORY-15
filesChanged:
  - web/ingest-control-plane
  - src/MediaIngest.Contracts/Workflow
  - src/MediaIngest.Api
  - tests/MediaIngest.Api.Tests
validation:
  - command: make test-ui
    outcome: pass/fail/not-run
  - command: git diff --check
    outcome: pass/fail/not-run
docsUpdated:
  - docs/architecture/004-observability-and-ui.md
  - docs/status/work-log.md
blockers:
  - none or details
next:
  - Use the orchestrator-backed graph as the baseline for future runtime status and diagnostics work.
```
