# User Stories

## Ingest Foundation

### Watch ingest mount

As an ingest operator, I want the system to monitor a configured mounted
directory, so that new media packages are detected automatically.

Acceptance themes:

- Path is configuration-driven.
- Local development can use local filesystem storage.
- Production storage backing is hidden behind the mount.

### Start only when manifest exists

As an ingest producer, I want ingest to start only after `manifest.json` exists,
so that incomplete package directories are not processed prematurely.

### Ingest all discovered files

As a MAM operator, I want the system to ingest every file physically present in
the package directory, so that incorrect manifests do not cause files to be
skipped.

### Reconcile on done marker

As an ingest operator, I want the system to rescan a package when the done marker
appears, so that late-arriving files are not missed.

## Routing And Agents

### Classify media essences

As an ingest system, I want to classify discovered files by essence type, so
that video, audio, text, and sidecar files can be routed to specialized agents.

### Route work through ASB queues

As a platform engineer, I want file processing commands routed to named Azure
Service Bus queues, so that each specialized agent only consumes work it owns.

### Process with specialized agents

As an operator, I want specialized agents for video, audio, text, proxy, and
other processing, so that each processing concern can scale and fail
independently.

## Reliability And Workflow

### Use transactional outbox

As a backend engineer, I want state changes and outbound messages committed
atomically, so that ingest work is not lost when services crash.

### Orchestrate package lifecycle with Dapr

As an ingest operator, I want a durable workflow per ingest package, so that
package progress survives crashes and can coordinate asynchronous agents.

### Support nested workflows

As a workflow operator, I want package workflows to drill into child workflows,
so that complex ingest processing remains understandable.

## Observability And UI

### Record agent progress

As an operator, I want each agent to record business progress and structured
logs with `workflowInstanceId`, so that processing can be audited and
correlated.

### Visualize workflow execution

As an operator, I want to see workflow nodes change color by status, so that I
can quickly understand what is pending, running, failed, or complete.

### Inspect node logs

As an operator, I want to click a workflow node and view its logs and timeline,
so that I can diagnose failures without searching raw logs manually.

### Drill into nested workflows

As an operator, I want to drill into child workflow graphs and navigate back to
parent workflows, so that nested processing remains explorable.

## Platform

### Deploy to Kubernetes

As a platform engineer, I want all services containerized and deployable to
Kubernetes, so that the system matches a cloud-native Azure runtime model.

