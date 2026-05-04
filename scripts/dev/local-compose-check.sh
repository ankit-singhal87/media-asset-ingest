#!/bin/sh
set -eu

script_dir=$(CDPATH= cd "$(dirname "$0")" && pwd)
repo_root=$(CDPATH= cd "$script_dir/../.." && pwd)
compose_file="deploy/docker/compose.yaml"

api_port="${LOCAL_COMPOSE_API_PORT:-5000}"
ui_port="${LOCAL_COMPOSE_UI_PORT:-5173}"
postgres_port="${LOCAL_COMPOSE_POSTGRES_PORT:-5432}"
compose_uid="${LOCAL_COMPOSE_UID:-$(id -u)}"
compose_gid="${LOCAL_COMPOSE_GID:-$(id -g)}"
api_url="${LOCAL_COMPOSE_API_URL:-http://127.0.0.1:$api_port}"
ui_url="${LOCAL_COMPOSE_UI_URL:-http://127.0.0.1:$ui_port}"
timeout_seconds="${LOCAL_COMPOSE_TIMEOUT_SECONDS:-60}"
interval_seconds="${LOCAL_COMPOSE_INTERVAL_SECONDS:-2}"
project_name="${LOCAL_COMPOSE_PROJECT_NAME:-media-asset-ingest-local}"
keep_running="${LOCAL_COMPOSE_KEEP_RUNNING:-0}"
dry_run=0
runtime_smoke=0
runtime_started=0

usage() {
  cat <<'USAGE'
Usage: sh scripts/dev/local-compose-check.sh [--dry-run] [--runtime-smoke]

Validates the local Docker Compose file without starting containers by default.

Options:
  --dry-run         Print the selected validation plan.
  --runtime-smoke   Start the local Compose runtime, wait for API/UI HTTP
                    responses, run scripts/dev/local-e2e-smoke.sh, and stop
                    the stack unless LOCAL_COMPOSE_KEEP_RUNNING=1 is set.

Environment overrides:
  LOCAL_COMPOSE_API_URL           API root URL. Default: http://127.0.0.1:5000
  LOCAL_COMPOSE_UI_URL            UI root URL. Default: http://127.0.0.1:5173
  LOCAL_COMPOSE_API_PORT          API host port. Default: 5000
  LOCAL_COMPOSE_UI_PORT           UI host port. Default: 5173
  LOCAL_COMPOSE_POSTGRES_PORT     PostgreSQL host port. Default: 5432
  LOCAL_COMPOSE_UID               API container user ID. Default: current user
  LOCAL_COMPOSE_GID               API container group ID. Default: current group
  LOCAL_COMPOSE_TIMEOUT_SECONDS   HTTP wait timeout. Default: 60
  LOCAL_COMPOSE_INTERVAL_SECONDS  HTTP wait interval. Default: 2
  LOCAL_COMPOSE_PROJECT_NAME      Compose project name. Default: media-asset-ingest-local
  LOCAL_COMPOSE_KEEP_RUNNING      Keep stack running after runtime smoke. Default: 0
USAGE
}

print_plan() {
  if [ "$runtime_smoke" -eq 1 ]; then
    cat <<PLAN
Local Compose runtime smoke plan

Compose file:
  deploy/docker/compose.yaml

Runtime services:
  api
    Builds deploy/docker/api.Dockerfile, exposes $api_url,
    bind-mounts ./input and ./output as plain ingest paths, and runs as
    UID:GID $compose_uid:$compose_gid for host-writable smoke artifacts.
  ui
    Builds deploy/docker/ui.Dockerfile and exposes $ui_url.
  postgres
    Uses postgres:17-alpine with local trust auth, exposes host port
    $postgres_port, and stores data in a named volume.

Runtime validation:
  docker compose -p $project_name -f deploy/docker/compose.yaml up --build -d
  wait for $api_url/api/ingest/status
  wait for $ui_url
  SMOKE_EXPECT_COPIED_FILES=manifest MEDIA_INGEST_API_URL=$api_url sh scripts/dev/local-e2e-smoke.sh
  docker compose -p $project_name -f deploy/docker/compose.yaml down

Boundary:
  This smoke starts only local Docker containers, uses repo-root input/output
  bind mounts, calls local HTTP endpoints, does not push images, access cloud
  resources, read secrets, create Azure resources, or perform paid actions.
PLAN
    return
  fi

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

require_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    printf '%s\n' "docker is required for local Compose validation." >&2
    exit 127
  fi
}

compose() {
  docker compose -p "$project_name" -f "$compose_file" "$@"
}

validate_settings() {
  case "$timeout_seconds" in
    ''|*[!0-9]*)
      printf 'LOCAL_COMPOSE_TIMEOUT_SECONDS must be a non-negative integer: %s\n' "$timeout_seconds" >&2
      exit 2
      ;;
  esac

  case "$interval_seconds" in
    ''|*[!0-9]*)
      printf 'LOCAL_COMPOSE_INTERVAL_SECONDS must be a positive integer: %s\n' "$interval_seconds" >&2
      exit 2
      ;;
    0)
      printf '%s\n' "LOCAL_COMPOSE_INTERVAL_SECONDS must be greater than 0." >&2
      exit 2
      ;;
  esac

  case "$api_url" in
    '')
      printf '%s\n' "LOCAL_COMPOSE_API_URL must not be empty." >&2
      exit 2
      ;;
  esac

  case "$ui_url" in
    '')
      printf '%s\n' "LOCAL_COMPOSE_UI_URL must not be empty." >&2
      exit 2
      ;;
  esac

  for port_setting in \
    "LOCAL_COMPOSE_API_PORT:$api_port" \
    "LOCAL_COMPOSE_UI_PORT:$ui_port" \
    "LOCAL_COMPOSE_POSTGRES_PORT:$postgres_port"
  do
    port_name=${port_setting%%:*}
    port_value=${port_setting#*:}

    case "$port_value" in
      ''|*[!0-9]*)
        printf '%s must be a positive integer: %s\n' "$port_name" "$port_value" >&2
        exit 2
        ;;
      0)
        printf '%s must be greater than 0.\n' "$port_name" >&2
        exit 2
        ;;
    esac
  done

  for id_setting in \
    "LOCAL_COMPOSE_UID:$compose_uid" \
    "LOCAL_COMPOSE_GID:$compose_gid"
  do
    id_name=${id_setting%%:*}
    id_value=${id_setting#*:}

    case "$id_value" in
      ''|*[!0-9]*)
        printf '%s must be a non-negative integer: %s\n' "$id_name" "$id_value" >&2
        exit 2
        ;;
    esac
  done
}

run_compose_config() {
  cd "$repo_root"
  require_docker

  printf '%s\n' "Validating local Compose configuration: $compose_file"
  docker compose -f "$compose_file" config

  printf '\n%s\n' "Resolved local services:"
  docker compose -f "$compose_file" config --services | sed 's/^/  - /'
}

cleanup_runtime_smoke() {
  if [ "$runtime_started" -eq 1 ] && [ "$keep_running" != "1" ]; then
    printf '%s\n' "Stopping local Compose runtime..."
    compose down
  fi
}

wait_for_http() {
  label="$1"
  url="$2"
  elapsed=0

  printf 'Waiting for %s: %s\n' "$label" "$url"
  while [ "$elapsed" -le "$timeout_seconds" ]; do
    if curl -fsS "$url" >/dev/null 2>&1; then
      printf '%s\n' "$label is responding."
      return 0
    fi

    if [ "$elapsed" -eq "$timeout_seconds" ]; then
      break
    fi

    sleep "$interval_seconds"
    elapsed=$((elapsed + interval_seconds))
  done

  printf 'Timed out waiting for %s after %s seconds: %s\n' "$label" "$timeout_seconds" "$url" >&2
  return 1
}

run_runtime_smoke() {
  cd "$repo_root"
  require_docker

  if ! command -v curl >/dev/null 2>&1; then
    printf '%s\n' "curl is required for local Compose runtime smoke validation." >&2
    exit 1
  fi

  mkdir -p input output

  trap cleanup_runtime_smoke EXIT INT TERM
  export LOCAL_COMPOSE_API_PORT="$api_port"
  export LOCAL_COMPOSE_UI_PORT="$ui_port"
  export LOCAL_COMPOSE_POSTGRES_PORT="$postgres_port"
  export LOCAL_COMPOSE_UID="$compose_uid"
  export LOCAL_COMPOSE_GID="$compose_gid"

  printf '%s\n' "Starting local Compose runtime: $compose_file"
  runtime_started=1
  compose up --build -d

  wait_for_http "API" "$api_url/api/ingest/status"
  wait_for_http "UI" "$ui_url"

  printf '%s\n' "Running local ingest smoke against Compose API..."
  SMOKE_EXPECT_COPIED_FILES=manifest MEDIA_INGEST_API_URL="$api_url" sh scripts/dev/local-e2e-smoke.sh

  printf '%s\n' "Local Compose runtime smoke passed."
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --dry-run)
      dry_run=1
      ;;
    --runtime-smoke)
      runtime_smoke=1
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      usage >&2
      exit 2
      ;;
  esac
  shift
done

validate_settings

if [ "$dry_run" -eq 1 ]; then
  print_plan
  exit 0
fi

if [ "$runtime_smoke" -eq 1 ]; then
  run_runtime_smoke
else
  run_compose_config
fi
