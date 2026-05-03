# ADR-0004: Use Message-Centric Orchestration

## Status

Accepted

## Context

The system will have multiple specialized agents. Agents should process messages
only from their respective queues.

## Decision

Use Dapr Workflow as the package coordination brain and Azure Service Bus as the
work distribution backbone.

## Consequences

- Agents remain independently deployable and scalable.
- Queue names define ownership boundaries.
- The outbox remains central to reliable command/event publication.
- Dapr activities are used selectively; agent work is primarily message-driven.

