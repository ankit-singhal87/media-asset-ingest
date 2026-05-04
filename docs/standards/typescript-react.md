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

## Package Validation

- Keep control-plane tests local to `web/ingest-control-plane` with Vitest and
  Testing Library.
- Run `npm test` and `npm run build` from `web/ingest-control-plane` when
  changing React behavior.
- The control-plane build uses the TypeScript 7 native preview through
  `@typescript/native-preview` and `tsgo`; do not reintroduce the JavaScript
  `typescript` compiler unless a compatibility task explicitly requires it.
- Keep package-local generated output such as `node_modules`, `dist`, coverage,
  and TypeScript build metadata out of Git.
