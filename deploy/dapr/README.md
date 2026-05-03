# Dapr Workflow Skeleton

This directory records the local Dapr lane for workflow runtime assets. TASK-5-1
does not add runtime components or Azure-backed state stores; those remain
deferred until the local runtime slice defines the Dapr sidecar wiring.

Current workflow skeleton:

- workflow name: `PackageIngestWorkflow`
- workflow application lane: `MediaIngest.Workflow`
- workflow instance id shape: `package-<packageId>`
- workflow runtime state remains separate from PostgreSQL business state
