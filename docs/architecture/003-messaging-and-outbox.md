# Messaging And Outbox

## Messaging

Use Azure Service Bus. Commands are published to semantic command topics and
routed to light, medium, or heavy command runners by filtered subscriptions.
This keeps command intent separate from execution capacity.

Initial command topics:

- `media.command.create_proxy`
- `media.command.create_checksum`
- `media.command.verify_checksum`
- `media.command.run_security_scan`
- `media.command.archive_asset`

Each command message carries an `executionClass` application property. Topic
subscriptions named `light`, `medium`, and `heavy` filter on that property, and
matching command-runner deployments consume only their subscription.

The first routing policy is:

- `light`: input size below 512 MB
- `medium`: input size from 512 MB through 10 GB
- `heavy`: input size above 10 GB

Checksum, verification, and security scan commands may cap at `medium` until a
later task proves they need heavy runners.

## Outbox

Use a transactional outbox in PostgreSQL. Business state changes and outbound
messages are committed in the same transaction. A dedicated outbox dispatcher
publishes pending messages to Azure Service Bus.

## Responsibility Split

- Domain/application logic decides which command should be sent and which
  execution class it needs.
- The outbox dispatcher publishes already-decided messages.
- The dispatcher does not classify files or make command-routing decisions.

## Reliability Requirements

- Messages must be idempotent.
- Consumers must tolerate at-least-once delivery.
- Dispatcher leasing must allow more than one dispatcher instance safely.
- Poison/dead-letter behavior must be observable.
- Topic/subscription backlog is the backpressure signal for Kubernetes scaling
  and runner concurrency.
