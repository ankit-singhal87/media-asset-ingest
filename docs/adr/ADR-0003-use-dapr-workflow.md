# ADR-0003: Use Dapr Workflow

## Status

Accepted

## Context

The system needs durable workflow orchestration for long-running media package
ingest. It should remain Kubernetes-native, cost-conscious, and friendly to .NET
workers.

## Decision

Use Dapr Workflow for package-level orchestration.

## Consequences

- Workflow definitions are code-first.
- Dapr runtime state is stored separately from business state.
- Temporal is deferred because paid managed service cost or heavier self-hosting
  is not ideal for this portfolio project.
- Camunda is deferred because BPMN/DMN visual authoring adds cost and
  operational weight that is not required for v1.
