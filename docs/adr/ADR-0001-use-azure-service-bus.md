# ADR-0001: Use Azure Service Bus

## Status

Accepted

## Context

The system needs durable asynchronous command routing between package
orchestration and specialized agents. Cloud target is Azure only.

## Decision

Use Azure Service Bus for brokered messaging.

Commands use named queues. Topics can be added later when event fan-out becomes
necessary.

## Consequences

- The production broker is Azure-native and managed.
- Agents can scale independently by queue.
- RabbitMQ is deferred because it would require self-management on AKS or a
  third-party managed service.
