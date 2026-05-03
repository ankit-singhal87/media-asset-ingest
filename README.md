# media-asset-ingest

Cloud-native media ingestion backend using Azure, messaging, observability, and identity patterns.

See [docs/README.md](docs/README.md) for architecture, product planning,
automation guidance, and ADRs.

## Quickstart

This repository is Linux-first and Docker-first. The default developer setup
keeps host installs small and runs application/runtime tooling in containers
where practical.

### 1. Check host tools

```bash
make check-tools
```

Required host tools are intentionally minimal:

- `bash`
- `git`
- `make`
- `curl`
- `jq`
- `node`
- `npm`
- `docker`

Optional tools such as `dotnet`, `kubectl`, `helm`, `dapr`, and `az` are useful
for direct host workflows, but the preferred project direction is to expose
containerized Makefile targets for those workflows as implementation begins.

### 2. Install or print tool guidance

```bash
make print-install-tools
```

To run the interactive Linux installer:

```bash
make install-tools
```

The installer does not log in to Azure or create paid cloud resources.

### 3. Validate the repository

```bash
make validate
```

At this stage validation checks documentation and shell script syntax. Runtime
build/test targets will be added as the .NET solution and containers are
introduced.

## Agent Tooling

This repository is operated with
[Codex CLI](https://github.com/openai/codex). Agents should start from
[AGENTS.md](AGENTS.md), then use
[docs/automation/README.md](docs/automation/README.md) for compact execution
context.

Enabled agent tools:

- [Superpowers plugin](https://github.com/obra/superpowers): use the
  repo-required planning, TDD, worktree, verification, and branch-finishing
  workflows.
- [GitHub plugin](https://github.com/openai/plugins): prefer it for structured
  issue, PR, review, diff, commit, and CI operations.
- `gh` and Make helpers: use these for GitHub Projects v2 fields, native
  sub-issue/dependency wiring, and tracker validation commands such as
  `make github-project-active`.

## Development Model

- Use BDD to frame user-visible behavior.
- Use TDD for behavior changes and bug fixes.
- Keep domain behavior independent of Dapr, Azure Service Bus, PostgreSQL, and
  UI infrastructure details.
- Update quickstart, automation, status, product, milestone, and tooling docs
  when a task changes developer workflow or project behavior.
- Keep GitHub Projects updated for roadmap, issue, milestone, task, bug, and PR
  state; repo docs keep durable standards and context.
