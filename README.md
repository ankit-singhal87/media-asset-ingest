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

Agent sessions should prefer the summary form for broad validation:

```bash
make validate-summary
```

It writes the full log under `/tmp` and prints only the command, exit code, key
success or failure lines, and log path.

Validation currently checks documentation, shell script syntax, GitHub tracker
helper wrappers, and the .NET solution smoke-test targets. Runtime-specific
checks such as local Compose validation are exposed as focused Make targets and
can be run when that slice is in scope.

### 4. Start or stop the local Docker runtime

Use Docker Compose for the current runnable local system:

```bash
make up
```

That target builds and starts the local API, UI, PostgreSQL, outbox worker, and
light/medium/heavy command-runner worker services with:

```bash
docker compose -f deploy/docker/compose.yaml up --build
```

Stop the local Docker runtime with:

```bash
make down
```

That target runs:

```bash
docker compose -f deploy/docker/compose.yaml down
```

Run the Docker-first runtime smoke when you need local runtime evidence beyond
static Compose validation:

```bash
make local-runtime-smoke
```

That target starts the API, UI, PostgreSQL, outbox worker, and command-runner
Compose stack, waits for local API and UI HTTP responses, runs the scripted
manifest ingest smoke against the containerized API, queries PostgreSQL for
durable package and dispatched command outbox rows, and stops the stack. The
smoke expects the copied manifest pair plus workflow command-node evidence for
media, sidecar, and metadata files. It uses only local containers, repo-root
`input/` and `output/` bind mounts, local HTTP endpoints, and PostgreSQL inside
Compose. Override `LOCAL_COMPOSE_API_PORT`, `LOCAL_COMPOSE_UI_PORT`, or
`LOCAL_COMPOSE_POSTGRES_PORT` if another local process already owns the default
ports. The smoke script runs the API container with the current host UID/GID so
bind-mounted smoke output remains writable by the host user.

## Local Manifest Ingest Demo

This host workflow exercises the manifest start path through the API, React
control plane, and repo-root runtime folders. The direct `dotnet run` path uses
in-memory persistence by default for focused local development. The Docker
Compose path uses PostgreSQL through raw Npgsql and runs the review worker
containers. Neither path uses the real Dapr Workflow runtime, Azure Service Bus,
or paid cloud resources. Ready packages are scanned recursively; the manifest
pair is copied to local output, and each non-metadata file becomes a locally
accepted command envelope with a semantic topic and `executionClass` for the
workflow graph demo.

Start the local ingest API on a fixed development port:

```bash
dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000
```

Start the UI in another terminal. The Vite development server proxies `/api`
requests to `http://127.0.0.1:5000`, so the UI can call the API as a
same-origin `/api/ingest/start` request.

```bash
cd web/ingest-control-plane
npm run dev
```

Open the Vite URL printed by the UI and press **Start ingest**. That starts the
local watcher against the repo-root `input/` folder. After the watcher is
running, create a package under `input/<asset>/`. The manifest contents are
metadata; the presence of both `manifest.json` and `manifest.json.checksum` is
the package start signal.

```bash
mkdir -p input/asset-001
printf '%s\n' '{"asset":"asset-001"}' > input/asset-001/manifest.json
printf '%s\n' 'local-demo-checksum' > input/asset-001/manifest.json.checksum
mkdir -p input/asset-001/media input/asset-001/sidecars
printf '%s\n' 'not-real-video' > input/asset-001/media/source.mov
printf '%s\n' 'not-real-caption' > input/asset-001/sidecars/caption.srt
```

The expected local output is the matching manifest pair under
`output/<asset>/`. The UI and workflow graph endpoints also show scan,
classification, dispatch, routed command work, reconcile, and finalize nodes.

```text
output/asset-001/manifest.json
output/asset-001/manifest.json.checksum
```

The `input/` and `output/` directories are local runtime folders. Backend runtime
setup owns the Git ignore rules for those folders.

For a script-backed smoke path that matches this flow, start the API and run:

```bash
sh scripts/dev/local-e2e-smoke.sh
```

To validate the smoke plan without starting the API or touching files:

```bash
sh scripts/dev/local-e2e-smoke.sh --dry-run
```

For the reviewable durable slice, prefer:

```bash
make local-runtime-smoke
```

Reviewer notes and source anchors live in
[docs/review/local-durable-ingest-review-guide.md](docs/review/local-durable-ingest-review-guide.md).
The current API contract lives in [docs/api/openapi.yaml](docs/api/openapi.yaml).

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
