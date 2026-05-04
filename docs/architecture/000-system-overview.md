# System Overview

## Purpose

Media Asset Ingest is a portfolio-grade cloud-native .NET backend for ingesting
media asset packages from a mounted filesystem path, coordinating asynchronous
processing through workflows and command topics, and exposing operational
visibility through a workflow graph UI.

## Target Architecture

- Runtime: Docker containers on Kubernetes.
- Cloud: Azure only.
- Orchestration: Dapr Workflow.
- Messaging: Azure Service Bus.
- Persistence: PostgreSQL.
- Reliability: transactional outbox.
- Observability: structured JSON logs, OpenTelemetry traces, and business
  progress/timeline state.
- UI: workflow graph control plane with nested workflow drilldown and node logs.

## Core Flow

1. A watcher monitors a configured directory such as `/mnt/ingest`.
2. Each immediate child directory is treated as an ingest package.
3. Work starts only after `manifest.json` and `manifest.json.checksum` exist.
4. The package scanner enumerates every file physically present in the directory.
5. The classifier records file metadata needed by command-routing policy.
6. The package workflow emits semantic commands through the outbox to Azure Service Bus.
7. Azure Service Bus topic subscriptions route commands to light, medium, or
   heavy command runners by `executionClass`.
8. Command runners record business progress and logs with shared correlation fields.
9. The done marker triggers reconciliation for late files.
10. The package workflow finalizes after required work completes.

## Key Separation

Dapr Workflow coordinates package state transitions. Azure Service Bus
distributes command work. Command runners execute supplied commands in
appropriately sized containers. PostgreSQL stores business truth. Dapr stores
workflow runtime state in its own state store.

## Runtime Readiness Boundary

The repository includes Docker and static Kubernetes readiness assets for the
API, UI, PostgreSQL dependency, Dapr sidecar configuration, Dapr workflow state
store, and Azure Service Bus pub/sub component. These assets document the
intended container runtime shape without creating Azure resources by default.

Kubernetes services stay internal by default with `ClusterIP` networking. Image
repository names, image tags, secret values, kubeconfigs, Azure subscription
details, and Terraform state are placeholders or omitted. Applying manifests to
an Azure cluster, creating Azure Service Bus, or binding managed storage remains
a manual step that requires explicit approval.

## Deferred Decisions

- Production mount backing: Azure Files, Blob CSI, or another Azure-backed option.
- Exact relational schema.
- Exact UI framework details beyond graph rendering requirements.
- Full Azure deployment and cost profile.
