# Docker Runtime Assets

This directory contains the current container build and local compose assets.

## Current Containers

- `api.Dockerfile` builds `src/MediaIngest.Api`.
- `ui.Dockerfile` builds `web/ingest-control-plane` and serves it through
  nginx.
- `compose.yaml` runs API, UI, and PostgreSQL for local development with the
  ingest input and output paths mounted as plain filesystem directories.

The watcher, workflow, outbox, command-runner, persistence, observability, and
essence projects are currently libraries or foundation slices rather than
standalone runnable container services. Add service Dockerfiles when those
projects gain executable hosts.

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
