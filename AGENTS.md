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
- Follow [docs/automation/execution-checklist.md](docs/automation/execution-checklist.md) before starting and before reporting completion.
- Follow [docs/automation/parallelization.md](docs/automation/parallelization.md) when dispatching parallel agents.
- Use [docs/automation/agent-handoff.md](docs/automation/agent-handoff.md) for task handoff results.
- Use [docs/automation/conflict-protocol.md](docs/automation/conflict-protocol.md) when scope, architecture, validation, or parallel work conflicts arise.
- Use [docs/automation/github-projects.md](docs/automation/github-projects.md) when changing GitHub issues, milestones, projects, bugs, or PR tracking state.
- Prefer the GitHub plugin for structured GitHub operations; use `gh` only for
  gaps such as Project board commands, CLI auth checks, or wrapper validation.
- Use [docs/automation/git-conventions.md](docs/automation/git-conventions.md) before creating branches, commits, or PRs.
- Follow [docs/standards/bdd-tdd.md](docs/standards/bdd-tdd.md) for behavior changes.
- Follow [docs/standards/domain-driven-design.md](docs/standards/domain-driven-design.md) for domain boundaries.
- Follow [docs/standards/tooling.md](docs/standards/tooling.md) when tools or commands change.
- Stop and ask before dependency changes, destructive Git operations, paid cloud
  actions, secret handling, or changes outside the assigned scope.
- Update ignored local `.worktrees/state/<worktree-slug>.md` records when
  creating, using, PR-opening, merging, or cleaning up worktrees.
- Keep GitHub Projects lightweight: issues and PRs are durable, and the Project
  is only a simple status board when remote tracker access is available.
- Start branch names, commit subjects, and PR titles with the lowest-level
  available work identifier: task ID, bug ID, then story ID.
- Local commits are allowed after relevant validation and should be small,
  scoped checkpoints. Pushes and PR creation happen only when the user asks or
  when a task explicitly grants automatic PR creation after validation.

## Architecture Principles

- Treat the ingest mount as a plain filesystem path from application code.
- Require `manifest.json` before package work starts.
- Treat the manifest as metadata and a start signal, not the authoritative file
  list.
- Ingest every file physically present in the package directory.
- Use Dapr Workflow for package orchestration and Azure Service Bus command
  topics/subscriptions for work distribution to generic command runners.
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
- satisfy the relevant definition of done in `docs/automation/definition-of-done.md`
- report validation evidence
- mention any tests or validations that could not be run
