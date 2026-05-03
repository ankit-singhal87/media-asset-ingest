# TypeScript And React Standards

## Direction

The workflow control plane UI should be operational, dense, and clear. Avoid a
marketing-style landing page. The first screen should help an operator inspect
ingest packages and workflow state.

## Principles

- Render workflow graphs from API-provided graph DTOs.
- Do not query logs as the source of node status.
- Use business state for graph colors and timeline.
- Keep node detail views focused on timeline, logs, identifiers, and failure
  context.
- Support nested workflow drilldown and back traversal.
- Prefer accessible color states with labels or icons where status ambiguity is
  possible.

