# Local Smoke Tests

## Manifest Ingest E2E

Use the manual UI flow or `scripts/dev/local-e2e-smoke.sh` to exercise the local
manifest ingest path from the repo-root `input/` folder to the repo-root
`output/` folder.

Start the API first:

```bash
dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000
```

### Manual UI Flow

Start the React control plane in another terminal:

```bash
cd web/ingest-control-plane
npm run dev
```

The Vite development server proxies `/api` requests to
`http://127.0.0.1:5000`. Open the Vite URL, press **Start ingest**, then create
the manifest package after the watcher has started:

```bash
mkdir -p input/asset-001
printf '%s\n' '{"asset":"asset-001"}' > input/asset-001/manifest.json
printf '%s\n' 'local-demo-checksum' > input/asset-001/manifest.json.checksum
```

The expected output is the matching manifest pair:

```text
output/asset-001/manifest.json
output/asset-001/manifest.json.checksum
```

### Scripted Smoke

Run the smoke script from another terminal after the API is listening:

```bash
sh scripts/dev/local-e2e-smoke.sh
```

The script resets only its selected `input/<asset>/` and `output/<asset>/`
directories, posts `POST /api/ingest/start`, creates `manifest.json` and
`manifest.json.checksum`, and adds sample media, sidecar, and metadata files
under `input/<asset>/`. It waits for matching files under `output/<asset>/`,
then verifies the ingest status includes the package workflow and that the
workflow graph includes command-node evidence.

For dry-run validation without starting the API or changing files:

```bash
sh scripts/dev/local-e2e-smoke.sh --dry-run
```

Dry-run mode prints the fixed local API endpoints, the selected package paths,
the file reset and creation plan, and the exact output assertions. It does not
start services, send HTTP requests, or change files.

Optional overrides:

- `MEDIA_INGEST_API_URL` changes the API root URL.
- `SMOKE_PACKAGE_ID` changes the package folder name.
- `SMOKE_TIMEOUT_SECONDS` changes the output polling timeout.
- `SMOKE_INTERVAL_SECONDS` changes the output polling interval.
- `SMOKE_EXPECT_COPIED_FILES` changes copied output assertions. The default
  `all` mode expects manifest, media, sidecar, and metadata files under
  `output/<asset>/`; `manifest` mode expects only the manifest pair while still
  verifying workflow command-node evidence.

## Local Compose Config

Use `make up` to start the local API, UI, and PostgreSQL Compose runtime from
the repository root:

```bash
make up
```

The target runs:

```bash
docker compose -f deploy/docker/compose.yaml up --build
```

Use `make down` to stop the local Compose runtime:

```bash
make down
```

The target runs:

```bash
docker compose -f deploy/docker/compose.yaml down
```

Use `scripts/dev/local-compose-check.sh` to validate the local API, UI, and
PostgreSQL Compose configuration without starting containers:

```bash
sh scripts/dev/local-compose-check.sh
```

The script runs:

```bash
docker compose -f deploy/docker/compose.yaml config
```

For a no-Docker plan that does not change files or start containers:

```bash
sh scripts/dev/local-compose-check.sh --dry-run
```

For a Docker-first runtime smoke that starts the local API, UI, and PostgreSQL
Compose stack, waits for API and UI HTTP responses, verifies the runnable
light/medium/heavy command-runner service boundaries, runs the scripted local
ingest smoke against the containerized API, queries command dispatch evidence,
and then stops the stack:

```bash
make local-runtime-smoke
```

The target runs:

```bash
sh scripts/dev/local-compose-check.sh --runtime-smoke
```

Use this command when the local Docker runtime slice is in scope. It uses only
local containers, repo-root `input/` and `output/` bind mounts, and local HTTP
endpoints; it does not push images, read secrets, create Azure resources, or
perform paid cloud actions. Set `LOCAL_COMPOSE_KEEP_RUNNING=1` to leave the
stack running after the smoke for manual inspection. Override
`LOCAL_COMPOSE_API_PORT`, `LOCAL_COMPOSE_UI_PORT`, or
`LOCAL_COMPOSE_POSTGRES_PORT` when another local process is already using the
default `5000`, `5173`, or `5432` ports. The script exports the current host
UID and GID as `LOCAL_COMPOSE_UID` and `LOCAL_COMPOSE_GID` so API-created
bind-mounted smoke files remain host-writable.

The Compose runtime smoke runs `local-e2e-smoke.sh` with
`SMOKE_EXPECT_COPIED_FILES=manifest` because the current local API copies the
manifest pair and represents media, sidecar, and metadata files as command
nodes. Full copied-file assertions remain available for smoke environments that
provide command-runner output behavior.

Command-runner evidence is intentionally local and boundary-level. The smoke
asserts that all three runner host containers are running, that each startup log
exposes its configured `executionClass`, and that dispatched command outbox rows
are persisted in PostgreSQL with execution-class routing metadata. The current
local runner hosts do not consume real Azure Service Bus messages or execute
media commands during this smoke.

For a no-Docker runtime-smoke plan:

```bash
sh scripts/dev/local-compose-check.sh --runtime-smoke --dry-run
```
