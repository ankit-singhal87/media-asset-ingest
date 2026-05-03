# Agent Guidance

Start with [docs/automation/README.md](docs/automation/README.md) for compact
automation guidance. Use [docs/product/user-stories.md](docs/product/user-stories.md)
and [docs/architecture/000-system-overview.md](docs/architecture/000-system-overview.md)
for product and architecture background unless the user explicitly changes
direction.

## Scope Boundaries

- This repository is a portfolio-grade cloud-native .NET media asset ingest
  backend.
- Cloud target is Azure only.
- The planned runtime is Docker containers on Kubernetes with Dapr Workflow,
  Azure Service Bus, PostgreSQL, and a transactional outbox.
- No load balancers, paid cloud actions, or production deployment are required
  for the initial planning and local foundation.
- Do not add `codex-workflows`, `.codex/agents`, or repo-local `.agents/skills`
  unless the user explicitly adopts that workflow later.

## Agent Execution Rules

- Keep edits inside the requested scope and relevant ownership lane.
- Do not full-scan the repo unless targeted context is insufficient.
- Read only the automation docs and slice docs relevant to the task.
- Do not let parallel work edit the same files at the same time.
- Follow [docs/product/task-workflow.md](docs/product/task-workflow.md) for task execution.
- Follow [docs/standards/bdd-tdd.md](docs/standards/bdd-tdd.md) for behavior changes.
- Follow [docs/standards/domain-driven-design.md](docs/standards/domain-driven-design.md) for domain boundaries.
- Follow [docs/standards/tooling.md](docs/standards/tooling.md) when tools or commands change.
- Stop and ask before dependency changes, destructive Git operations, paid cloud
  actions, secret handling, or changes outside the assigned scope.
- Commits and pushes happen only when the user asks.

## Architecture Principles

- Treat the ingest mount as a plain filesystem path from application code.
- Require `manifest.json` before package work starts.
- Treat the manifest as metadata and a start signal, not the authoritative file
  list.
- Ingest every file physically present in the package directory.
- Use Dapr Workflow for package orchestration and Azure Service Bus queues for
  work distribution to specialized agents.
- Keep PostgreSQL as the business source of truth.
- Keep Dapr workflow runtime state separate from business state.
- Use structured logs, OpenTelemetry traces, and business timeline events.

## Secrets

- Never commit secrets, credentials, private keys, tokens, `.env`, Terraform
  state, generated kubeconfigs, or cloud subscription details.
- Keep placeholders in examples; real values stay outside the repository.

## Completion Checklist

Before reporting completion for implementation or tooling tasks:

- update relevant product, status, standards, and automation docs
- run the cheapest relevant validation from `docs/automation/validation.md`
- report validation evidence
- mention any tests or validations that could not be run
