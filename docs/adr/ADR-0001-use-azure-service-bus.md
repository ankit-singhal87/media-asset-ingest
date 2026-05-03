# ADR-0001: Use Azure Service Bus

## Status

Accepted

## Context

The system needs durable asynchronous command routing between package
orchestration and command runners. Cloud target is Azure only.

## Decision

Use Azure Service Bus for brokered messaging.

Commands use semantic topics such as `media.command.create_proxy`. Filtered
subscriptions route each command to light, medium, or heavy command runners by
message properties such as `executionClass`.

## Consequences

- The production broker is Azure-native and managed.
- Command runners can scale independently by topic subscription backlog.
- Command intent stays decoupled from runner capacity and container sizing.
- RabbitMQ is deferred because it would require self-management on AKS or a
  third-party managed service.
