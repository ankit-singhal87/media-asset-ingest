# Domain-Driven Design Standards

## Domain Language

Use consistent terms:

- ingest package
- manifest
- done marker
- essence
- source/master video
- sidecar
- work item
- agent
- workflow instance
- workflow node
- package timeline

## Boundaries

Keep domain behavior separate from infrastructure.

- Domain/application code decides package lifecycle and routing intent.
- Infrastructure adapters talk to Dapr, Azure Service Bus, PostgreSQL, filesystems, and logging backends.
- Agents own specialized processing capabilities.
- UI projections read business state, not Dapr runtime internals or raw logs.

## Aggregates And Concepts

Initial domain concepts:

- Ingest package: one subdirectory under the ingest mount.
- Discovered file: one file physically present in a package.
- Work item: one processing request for a file or package.
- Workflow node: one UI-visible step in a workflow graph.
- Timeline event: business-facing record of something that happened.

Exact persistence schemas are deferred, but implementation should preserve these
conceptual boundaries.

## Rules

- Do not leak Azure Service Bus or Dapr types into domain model names.
- Do not make manifest file lists authoritative.
- Do not store UI status only in logs.
- Do not make workflow runtime state the business source of truth.
- Keep idempotency explicit at message and agent boundaries.

