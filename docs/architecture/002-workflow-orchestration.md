# Workflow Orchestration

## Decision

Use Dapr Workflow for durable package-level orchestration.

## Workflow Style

Use message-centric orchestration. Dapr coordinates lifecycle decisions, waits,
timers, child workflows, and finalization. Azure Service Bus queues distribute
actual work to independent agents.

## Root Workflow

The root workflow is `PackageIngestWorkflow`. It owns the package lifecycle:

1. manifest detected
2. package scan requested
3. file classification requested
4. work commands emitted
5. agent completion events observed
6. done marker observed or awaited
7. reconciliation requested
8. package finalized

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

## Parallelism

Dapr Workflow supports fan-out/fan-in. A package workflow may start multiple
child workflows or activities in parallel and wait for all required work to
finish before reconciliation or finalization.

## State Boundary

Dapr workflow runtime state is not business truth. Business status, timeline,
and audit state must be recorded in PostgreSQL application tables.
