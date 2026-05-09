# Docker Runtime Assets

This directory contains the current container build and local compose assets.

## Current Containers

- `api.Dockerfile` builds `src/MediaIngest.Api`.
- `ui.Dockerfile` builds `web/ingest-control-plane` and serves it through
  nginx.
- `compose.yaml` runs API, UI, and PostgreSQL for local development with the
  ingest input and output paths mounted as plain filesystem directories.

The watcher, workflow, outbox, command-runner, persistence, observability, and
essence projects are libraries or host-backed slices rather than standalone
container services unless they have explicit executable hosts. Add service
Dockerfiles when those projects gain new executable host boundaries.

## Boundary

Docker assets are local/runtime scaffolding. They do not create Azure resources,
publish images, store credentials, or validate a production deployment.

Start the local API, UI, and PostgreSQL Compose runtime from the repository
root:

```bash
make up
```

The target runs `docker compose -f deploy/docker/compose.yaml up --build`.
Stop the local Compose runtime with:

```bash
make down
```

The target runs `docker compose -f deploy/docker/compose.yaml down`.

Use the static local check from the repository root to validate Compose syntax
and service resolution without starting containers:

```bash
sh scripts/dev/local-compose-check.sh
```

The check runs `docker compose -f deploy/docker/compose.yaml config` and reports
the resolved API, UI, and PostgreSQL services. Use `--dry-run` to print the
planned validation boundary without requiring Docker.

Use the runtime smoke from the repository root when you need to exercise the
Docker-first local runtime:

```bash
make local-runtime-smoke
```

The target starts the Compose stack with image builds, waits for the API and UI
HTTP endpoints, runs `scripts/dev/local-e2e-smoke.sh` against the containerized
API with manifest-output assertions plus workflow command-node evidence, and
stops the stack afterward. Set `LOCAL_COMPOSE_KEEP_RUNNING=1` to keep the stack
up for manual inspection after the smoke. If the default local ports are
already in use, override `LOCAL_COMPOSE_API_PORT`, `LOCAL_COMPOSE_UI_PORT`, or
`LOCAL_COMPOSE_POSTGRES_PORT`. The smoke script also exports the current host
UID/GID for the API container so bind-mounted smoke output remains writable by
the host user.
