# Milestones

## Milestone 1: Documentation And Local Foundation

- Automation context and guardrails.
- Architecture overview.
- ADRs.
- Product stories.
- Initial standards.
- Status, task workflow, and work log.
- Linux tool check/install guidance.
- Makefile and npm validation entrypoints.

## Milestone 2: .NET Solution And Local Runtime

- .NET solution structure.
- Docker Compose or equivalent local services.
- PostgreSQL local database.
- Basic health checks.

## Milestone 3: Ingest Package Lifecycle

- Filesystem watcher.
- Manifest gating.
- Package scan.
- Done marker reconciliation.
- File classification.

## Milestone 4: Messaging And Outbox

- PostgreSQL transactional outbox.
- Outbox dispatcher.
- Azure Service Bus abstraction.
- Local broker development strategy.

## Milestone 5: Dapr Workflow Orchestration

- Package ingest workflow.
- Child workflow boundaries.
- Workflow events and signals.
- Workflow state separation.

## Milestone 6: Specialized Agents

- Video agent.
- Audio agent.
- Text agent.
- Other/sidecar agent.
- Proxy creation agent.

## Milestone 7: Observability

- Structured JSON logs.
- OpenTelemetry traces.
- Correlation fields.
- Business timeline/progress events.

## Milestone 8: Workflow Visualization UI

- Workflow graph API.
- Node status projection.
- React graph UI.
- Node details and logs.
- Nested workflow drilldown and back traversal.

## Milestone 9: Kubernetes And Azure Readiness

- Kubernetes manifests.
- Dapr component definitions.
- Azure Service Bus configuration docs.
- Azure PostgreSQL configuration docs.
- Cost-conscious deployment notes.
