# TASK-8-1: Scaffold React Workflow Control Plane

## Status

Planned

## Linked Work

- GitHub issue: #33
- Milestone: MILESTONE-8
- User stories:
  - USER-STORY-12
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #22
- Blocked by: None
- Blocks: node log and nested workflow UI tasks.

## Ownership

Primary lane: Canvas

Supporting lanes:

- Gauge

## Target Files

Agents may edit only these files unless they escalate:

- `web/ingest-control-plane`
- `docs/plans/task-index.md`
- `docs/plans/active-worktrees.md`
- `docs/status/work-log.md`
- `docs/standards/typescript-react.md`

## BDD Scenario

```gherkin
Scenario: Operator sees a mocked workflow graph
  Given mocked workflow graph data
  When the control plane loads
  Then the UI displays workflow nodes and enough state to inspect progress
```

## Validation

Minimal validation:

```bash
make validate
git diff --check
```
