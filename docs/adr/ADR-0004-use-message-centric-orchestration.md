# ADR-0004: Use Message-Centric Orchestration

## Status

Accepted

## Context

The system will have multiple command-runner capacity classes. Runners should
process messages only from their light, medium, or heavy subscriptions.

## Decision

Use Dapr Workflow as the package coordination brain and Azure Service Bus as the
work distribution backbone.

## Consequences

- Agents remain independently deployable and scalable.
- Command topic names define business intent; filtered subscriptions define
  execution capacity boundaries.
- The outbox remains central to reliable command/event publication.
- Dapr activities are used selectively; command work is primarily message-driven.
