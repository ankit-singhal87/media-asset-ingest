# Workflow Orchestration

## Decision

Use Dapr Workflow for durable package-level orchestration.

## Workflow Style

Use message-centric orchestration. Dapr coordinates lifecycle decisions, waits,
timers, child workflows, and finalization. Azure Service Bus command topics
distribute actual work to independent command runners.

`MediaIngest.Workflow.Orchestrator` is the standalone bounded-context assembly
for orchestrator-owned workflow code. The current slice exposes the boundary
marker used by later catalog discovery work. Real Dapr Workflow SDK hosting and
workflow/activity registration remain deferred until a dependency-approved
task adds the runtime package references.

## Root Workflow

The root workflow is `PackageIngestWorkflow`. It owns the package lifecycle:

1. manifest detected
2. package scan requested
3. file classification requested
4. semantic work commands emitted
5. command completion events observed
6. done marker observed or awaited
7. reconciliation requested
8. package finalized

Shared contract constants reserve the root workflow name
`PackageIngestWorkflow` and child workflow names for package scan, file
classification, essence group processing, proxy creation, reconciliation, and
finalization. Workflow implementations and UI projections should use those names
when publishing or rendering graph state.

## Child Workflows

Child workflows are allowed for meaningful lifecycle chunks:

- package scan
- file classification
- essence group processing
- proxy creation
- reconciliation
- finalization

Do not make every tiny operation its own workflow. Use child workflows when the
boundary improves clarity, retry behavior, or UI drilldown.

Prepared child workflow work includes the child workflow name, stable child
workflow instance ID, and parent workflow instance ID before runtime execution.
The stable child ID format is `<parent-workflow-instance-id>/<child-node-id>`.
Graph projections must use these prepared references when they are available so
operator drilldown matches the identifiers used to start child workflows.

## Parallelism

Dapr Workflow supports fan-out/fan-in. A package workflow may start multiple
child workflows or activities in parallel and wait for all required work to
finish before reconciliation or finalization.

## State Boundary

Dapr workflow runtime state is not business truth. Business status, timeline,
and audit state must be recorded in PostgreSQL application tables.
