# Observability And Workflow UI

## Observability Layers

Use three layers:

1. Structured JSON logs for diagnostics.
2. OpenTelemetry traces for distributed correlation.
3. PostgreSQL business state and timeline events for UI and audit.

Logs are not the source of truth for UI status.

## Correlation Fields

Every service and agent should include:

- `workflowInstanceId`
- `packageId`
- `fileId`
- `workItemId`
- `nodeId`
- `agentType`
- `queueName`
- `correlationId`
- `causationId`
- `traceId`
- `spanId`

## Workflow UI

The UI renders a graph model, not a generated bitmap. Nodes represent workflow
steps, child workflows, or work items. Edges represent execution flow and
dependencies.

Node states:

- pending: grey
- queued: light grey or outlined
- running: blue
- succeeded: green
- failed: red
- waiting: amber
- skipped: muted
- cancelled: dark grey

## Drilldown

Nested workflows must support drilldown and back traversal. A user can enter a
child workflow graph, inspect nodes, then return to the parent workflow graph.

## Node Details

Clicking a node should show:

- business timeline entries
- agent logs filtered by correlation fields
- work item metadata
- queue/message identifiers where available
- trace link or trace identifiers where available

The UI should initially poll APIs for graph state. SignalR can be added later
for live updates.
