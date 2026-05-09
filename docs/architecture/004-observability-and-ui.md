# Observability And Workflow UI

## Observability Layers

Use three layers:

1. Structured JSON logs for diagnostics.
2. OpenTelemetry traces for distributed correlation.
3. PostgreSQL business state and timeline events for UI and audit.

Logs are not the source of truth for UI status.

## Correlation Fields

Every service and command runner should use the canonical field names from
`MediaIngest.Observability.CorrelationFieldNames` when emitting structured logs,
timeline events, trace attributes, and UI diagnostic projections.

The shared field names are:

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

`ObservabilityCorrelationContext` provides the foundation object for carrying
these identifiers across watcher, workflow, command runner, broker, timeline, and UI
diagnostic boundaries. Runtime adapters may enrich the context from message
metadata or trace state, but they should not rename these fields.

## Workflow UI

The UI renders a graph model, not a generated bitmap. Nodes represent workflow
steps, child workflows, work items, waits, command dispatch points, command
completion points, or finalization points. Edges represent execution flow and
dependencies.

Shared backend/UI contracts live in `MediaIngest.Contracts.Workflow`.
`WorkflowGraphDto` identifies the workflow instance, workflow name, package,
optional parent workflow instance, graph nodes, and graph edges. `WorkflowNodeDto`
uses stable node IDs, display names, node kinds, business statuses, workflow and
package correlation, optional work item IDs, and optional child workflow
instance IDs for drilldown. `WorkflowEdgeDto` links source and target node IDs.
Orchestrator workflow definitions are the durable source for static graph
topology; API projections overlay business status and command work-item
instances on top of that topology.

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
- command-runner logs filtered by correlation fields
- work item metadata
- queue/message identifiers where available
- trace link or trace identifiers where available

`WorkflowNodeDetailsDto` carries node-scoped business timeline and log entries.
Timeline and log DTOs include correlation IDs; log DTOs can also carry trace and
span identifiers. These DTOs are UI projections over business state and
diagnostic records, not Dapr runtime state.

The UI should initially poll APIs for graph state. SignalR can be added later
for live updates.
