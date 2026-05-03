# Agent Context

- Portfolio-grade cloud-native .NET backend for media asset ingest.
- Cloud target: Azure only.
- Runtime target: Docker containers on Kubernetes, initially without load balancers.
- Orchestration: Dapr Workflow.
- Messaging: Azure Service Bus queues for commands, topics later when fan-out events are needed.
- Data: PostgreSQL for business state and transactional outbox.
- Dapr state: separate PostgreSQL-backed state store for workflow runtime data.
- Ingest source: configured filesystem path such as `/mnt/ingest`.
- Local dev mount: local directory bind-mounted into containers.
- Production mount: Azure-backed storage decision deferred; app code sees a plain path.
- UI direction: workflow graph visualization with nested workflow drilldown and node logs.
- Development process: BDD for behavior framing, TDD for implementation, DDD for domain boundaries.
- Tooling process: Linux-first, Docker-first, Makefile entrypoints, npm for
  repo-level checks, and shell scripts for host tool detection/installation guidance.
