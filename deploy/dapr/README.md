# Dapr Workflow Runtime Assets

This directory records the local Dapr lane for workflow runtime assets. TASK-5-1
added the workflow skeleton. USER-STORY-17 adds static Kubernetes-oriented Dapr
component templates while keeping cloud execution deferred.

Current workflow skeleton:

- workflow name: `PackageIngestWorkflow`
- workflow application lane: `MediaIngest.Workflow`
- workflow instance id shape: `package-<packageId>`
- workflow runtime state remains separate from PostgreSQL business state

Kubernetes component templates:

- `k8s/configuration.yaml` defines the Dapr sidecar configuration name used by
  Kubernetes workload annotations.
- `k8s/postgres-state.yaml` defines the Dapr workflow state store as a
  PostgreSQL-backed component named `workflowstatestore`.
- `k8s/servicebus-pubsub.yaml` defines the Azure Service Bus topic pub/sub
  component named `commandbus`.
- `k8s/kustomization.yaml` lets agents render the Dapr assets without contacting
  a Kubernetes API server.

Static validation:

```bash
kubectl kustomize deploy/dapr/k8s
```

This render command does not apply resources, read kubeconfig, create Azure
Service Bus resources, or require paid cloud execution.

Command routing boundary:

| Command intent | Topic name | Execution property |
| --- | --- | --- |
| Create proxy | `media.command.create_proxy` | `executionClass` |
| Create checksum | `media.command.create_checksum` | `executionClass` |
| Verify checksum | `media.command.verify_checksum` | `executionClass` |
| Run security scan | `media.command.run_security_scan` | `executionClass` |
| Archive asset | `media.command.archive_asset` | `executionClass` |

`executionClass` values are stable lower-case strings: `light`, `medium`, and
`heavy`. Azure Service Bus subscriptions are named for those values and filter
on the application property; command topic names stay semantic and do not encode
runner capacity.

TASK-4-3 defines static topology readiness only. The command contracts enumerate
the semantic topics and the `light`, `medium`, and `heavy` subscription filters
that Azure Service Bus resources must eventually use, but these Dapr assets do
not create topics or subscriptions and do not require paid Azure validation.

USER-STORY-6 adds the local outbox publisher boundary through the Dapr sidecar
HTTP API. The outbox worker publishes each pending message to
`/v1.0/publish/commandbus/{topic}`, where `{topic}` is the outbox message
destination. Existing outbox application properties, including
`executionClass`, are forwarded as Dapr publish metadata query parameters such
as `metadata.executionClass=heavy`. This keeps the local publisher independent
of Azure SDKs and Azure resource provisioning.

All credentials are Kubernetes Secret references. Real secret values,
kubeconfigs, Azure subscription details, and Terraform state are intentionally
absent from this repository.
