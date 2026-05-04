#!/bin/sh
set -eu

script_dir=$(CDPATH= cd "$(dirname "$0")" && pwd)
repo_root=$(CDPATH= cd "$script_dir/../.." && pwd)
compose_file="deploy/docker/compose.yaml"

usage() {
  cat <<'USAGE'
Usage: sh scripts/dev/local-compose-check.sh [--dry-run]

Validates the local Docker Compose file without starting containers.
USAGE
}

print_plan() {
  cat <<'PLAN'
Local Compose validation plan

Compose file:
  deploy/docker/compose.yaml

Resolved local services:
  api
    Builds deploy/docker/api.Dockerfile, exposes http://127.0.0.1:5000,
    and bind-mounts ./input and ./output as plain ingest paths.
  ui
    Builds deploy/docker/ui.Dockerfile and exposes http://127.0.0.1:5173.
  postgres
    Uses postgres:17-alpine with local trust auth, exposes 127.0.0.1:5432,
    and stores data in the postgres-data named volume.

Default validation:
  docker compose -f deploy/docker/compose.yaml config

Boundary:
  This check does not start containers, push images, access cloud resources,
  read secrets, create Azure resources, or perform paid actions.
PLAN
}

run_compose_config() {
  cd "$repo_root"

  if ! command -v docker >/dev/null 2>&1; then
    printf '%s\n' "docker is required for local Compose validation." >&2
    exit 127
  fi

  printf '%s\n' "Validating local Compose configuration: $compose_file"
  docker compose -f "$compose_file" config

  printf '\n%s\n' "Resolved local services:"
  docker compose -f "$compose_file" config --services | sed 's/^/  - /'
}

case "${1:-}" in
  "")
    run_compose_config
    ;;
  --dry-run)
    print_plan
    ;;
  -h|--help)
    usage
    ;;
  *)
    usage >&2
    exit 2
    ;;
esac
