# .NET Standards

## Direction

Use modern .NET worker services and APIs with clear boundaries between workflow,
messaging, persistence, agents, and observability.

## Principles

- Prefer explicit contracts for commands, events, and API DTOs.
- Keep agents focused on one processing capability.
- Keep workflow code orchestration-focused; do not hide business state only in
  workflow runtime data.
- Make message handlers idempotent.
- Use dependency injection for infrastructure boundaries.
- Use structured logging with correlation fields.
- Prefer focused tests around domain behavior, message handling, and persistence
  boundaries.
- Follow [BDD and TDD standards](bdd-tdd.md) for behavior changes.
- Follow [DDD standards](domain-driven-design.md) for domain boundaries.

## Naming

- Use `workflowInstanceId` for Dapr workflow instance correlation.
- Use `packageId` for ingest package identity.
- Use `fileId` for discovered file identity.
- Use `workItemId` for processing work identity.
- Use `nodeId` for workflow graph node identity.
