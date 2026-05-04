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

The static command-bus topology is represented in the command contracts so
application, outbox, and deployment slices can validate the same semantic topic
and subscription names before Azure resources exist. This readiness model is a
contract and documentation boundary only: it does not provision Azure Service Bus
topics, create subscriptions, or perform paid cloud validation.

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

The local publisher boundary uses Dapr pub/sub over the sidecar HTTP API instead
of an Azure SDK. The dispatcher passes an `OutboxPublishRequest` to an
`IOutboxMessagePublisher`; the Dapr implementation posts the message JSON to
`/v1.0/publish/commandbus/{destination}`, where `destination` is the semantic
command topic stored on the outbox message. Application properties that were
already computed for the outbox request are forwarded as Dapr metadata query
parameters, so command metadata such as `executionClass=heavy` reaches the
broker boundary without the dispatcher making routing decisions.

The Azure Service Bus adapter boundary is represented by the outbox worker's
command-bus adapter. It maps an `OutboxPublishRequest` for a
`MediaCommandEnvelope` into a broker-oriented message shape with:

- the semantic Service Bus topic from the outbox destination;
- the raw JSON command envelope body;
- application properties copied from the outbox publish request; and
- the routed subscription name selected from the static `executionClass`
  topology.

Local development keeps the same dispatcher behavior by validating that
Service Bus mapping first, then delegating publication to the existing local
publisher strategy. Today that local strategy is the Dapr sidecar publisher,
but tests may also use in-memory publishers behind the same
`IOutboxMessagePublisher` boundary. This keeps command topic and application
property rules exercised locally without adding an Azure SDK dependency.

This boundary is local runtime plumbing only. It does not provision Azure
Service Bus topics, create subscriptions, run paid cloud validation, or add Azure
SDK dependencies.

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
