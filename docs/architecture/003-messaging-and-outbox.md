# Messaging And Outbox

## Messaging

Use Azure Service Bus. Commands are routed to named queues. Topics can be added
later for integration events that need fan-out.

Initial queues:

- `ingest.package.scan`
- `ingest.package.reconcile`
- `ingest.package.finalize`
- `ingest.file.video`
- `ingest.file.audio`
- `ingest.file.text`
- `ingest.file.other`

Agents consume only their assigned queues.

## Outbox

Use a transactional outbox in PostgreSQL. Business state changes and outbound
messages are committed in the same transaction. A dedicated outbox dispatcher
publishes pending messages to Azure Service Bus.

## Responsibility Split

- Domain/application logic decides which message should be sent and which queue
  it targets.
- The outbox dispatcher publishes already-decided messages.
- The dispatcher does not classify files or make domain routing decisions.

## Reliability Requirements

- Messages must be idempotent.
- Consumers must tolerate at-least-once delivery.
- Dispatcher leasing must allow more than one dispatcher instance safely.
- Poison/dead-letter behavior must be observable.
