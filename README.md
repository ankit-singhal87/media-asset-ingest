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

## Local Manifest Ingest Demo

This draft workflow is an in-process local demo. It exercises the manifest start
path through local folders and does not yet use real Dapr Workflow runtime,
PostgreSQL, Azure Service Bus, or command runner services.

Start the local ingest API on a fixed development port:

```bash
dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000
```

Start the UI in another terminal. The UI sends `POST /api/ingest/start` as a
same-origin request, so the Vite development server must proxy `/api` requests
to `http://127.0.0.1:5000`.

```bash
cd web/ingest-control-plane
npm run dev
```

Open the Vite URL printed by the UI and press **Start ingest**. That starts the
local watcher against the repo-root `input/` folder. After the watcher is
running, create a package under the local runtime input folder. The manifest
contents are opaque to this slice; the presence of `manifest.json` and
`manifest.json.checksum` is the package start signal.

```bash
mkdir -p input/asset-001
printf '%s\n' '{"asset":"asset-001"}' > input/asset-001/manifest.json
printf '%s\n' 'local-demo-checksum' > input/asset-001/manifest.json.checksum
```

The expected local output is:

```text
output/asset-001/manifest.json
output/asset-001/manifest.json.checksum
```

The `input/` and `output/` directories are local runtime folders. Backend runtime
setup owns the Git ignore rules for those folders.

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
  repository, issue, PR, review, diff, commit, CI, comment, label, and merge
  operations.
- `gh` and Make helpers: use these only when the plugin cannot cover the
  action, especially GitHub Projects v2 fields, CLI auth checks, and tracker
  validation commands such as
  `make github-project-active`.

## Development Model

- Use BDD to frame user-visible behavior.
- Use TDD for behavior changes and bug fixes.
- Keep domain behavior independent of Dapr, Azure Service Bus, PostgreSQL, and
  UI infrastructure details.
- Update quickstart, automation, status, product, milestone, and tooling docs
  when a task changes developer workflow or project behavior.
- Keep GitHub issues and PRs as durable remote tracking. Use the GitHub Project
  only as a lightweight status board; repo docs keep durable standards and
  context.
