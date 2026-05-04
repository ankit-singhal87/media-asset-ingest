#!/usr/bin/env bash
set -eu

api_url="${MEDIA_INGEST_API_URL:-http://127.0.0.1:5000}"
package_id="${SMOKE_PACKAGE_ID:-asset-smoke-001}"
timeout_seconds="${SMOKE_TIMEOUT_SECONDS:-20}"
interval_seconds="${SMOKE_INTERVAL_SECONDS:-1}"
dry_run=0

usage() {
  printf '%s\n' "Usage: sh scripts/dev/local-e2e-smoke.sh [--dry-run]"
  printf '%s\n' ""
  printf '%s\n' "Environment overrides:"
  printf '%s\n' "  MEDIA_INGEST_API_URL       API root URL. Default: http://127.0.0.1:5000"
  printf '%s\n' "  SMOKE_PACKAGE_ID           Package ID to create. Default: asset-smoke-001"
  printf '%s\n' "  SMOKE_TIMEOUT_SECONDS      Output polling timeout. Default: 20"
  printf '%s\n' "  SMOKE_INTERVAL_SECONDS     Output polling interval. Default: 1"
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --dry-run)
      dry_run=1
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      printf 'Unknown argument: %s\n' "$1" >&2
      usage >&2
      exit 2
      ;;
  esac
  shift
done

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repo_root=$(CDPATH= cd -- "$script_dir/../.." && pwd)
input_root="$repo_root/input"
output_root="$repo_root/output"
package_input="$input_root/$package_id"
package_output="$output_root/$package_id"
manifest_input="$package_input/manifest.json"
checksum_input="$package_input/manifest.json.checksum"
manifest_output="$package_output/manifest.json"
checksum_output="$package_output/manifest.json.checksum"
status_json="$repo_root/.tmp-local-e2e-status.json"
graph_json="$repo_root/.tmp-local-e2e-graph.json"

print_plan() {
  printf '%s\n' "Local ingest smoke plan:"
  printf '  %-18s %s\n' "api_url" "$api_url"
  printf '  %-18s %s\n' "package_id" "$package_id"
  printf '  %-18s %s\n' "input_package" "$package_input"
  printf '  %-18s %s\n' "output_package" "$package_output"
  printf '  %-18s %s seconds\n' "timeout" "$timeout_seconds"
}

if [ "$dry_run" -eq 1 ]; then
  print_plan
  printf '%s\n' "Dry run only. No files were changed and no HTTP request was sent."
  exit 0
fi

case "$timeout_seconds" in
  ''|*[!0-9]*)
    printf 'SMOKE_TIMEOUT_SECONDS must be a non-negative integer: %s\n' "$timeout_seconds" >&2
    exit 2
    ;;
esac

case "$interval_seconds" in
  ''|*[!0-9]*)
    printf 'SMOKE_INTERVAL_SECONDS must be a positive integer: %s\n' "$interval_seconds" >&2
    exit 2
    ;;
  0)
    printf '%s\n' "SMOKE_INTERVAL_SECONDS must be greater than 0." >&2
    exit 2
    ;;
esac

if ! command -v curl >/dev/null 2>&1; then
  printf '%s\n' "curl is required for the local ingest smoke script." >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  printf '%s\n' "jq is required for the local ingest smoke script." >&2
  exit 1
fi

print_plan

printf '%s\n' "Resetting smoke package folders..."
rm -rf -- "$package_input" "$package_output" "$status_json" "$graph_json"
mkdir -p -- "$package_input/media" "$package_input/sidecars" "$output_root"

printf '%s\n' "Posting ingest start..."
curl -fsS -X POST "$api_url/api/ingest/start" >/dev/null

printf '%s\n' "Creating package manifest files..."
printf '{"asset":"%s"}\n' "$package_id" > "$manifest_input"
printf '%s\n' "local-smoke-checksum" > "$checksum_input"
printf '%s\n' "not-real-video" > "$package_input/media/source.mov"
printf '%s\n' "not-real-audio" > "$package_input/media/mix.wav"
printf '%s\n' "not-real-caption" > "$package_input/sidecars/caption.srt"
printf '%s\n' "opaque metadata" > "$package_input/notes.bin"

elapsed=0
while [ "$elapsed" -le "$timeout_seconds" ]; do
  if [ -f "$manifest_output" ] && [ -f "$checksum_output" ]; then
    curl -fsS "$api_url/api/ingest/status" > "$status_json"
    workflow_id=$(jq -r --arg package_id "$package_id" '.packages[] | select(.packageId == $package_id) | .workflowInstanceId' "$status_json")

    if [ -n "$workflow_id" ]; then
      curl -fsS "$api_url/api/workflows/$workflow_id/graph" > "$graph_json"
      command_node_count=$(jq '[.nodes[] | select(.nodeId | startswith("command-"))] | length' "$graph_json")

      if [ "$command_node_count" -ge 4 ]; then
        printf '%s\n' "Local ingest smoke passed."
        printf '%s\n' "Observed output files:"
        printf '  %s\n' "$manifest_output"
        printf '  %s\n' "$checksum_output"
        printf '  workflow: %s\n' "$workflow_id"
        printf '  command_nodes: %s\n' "$command_node_count"
        rm -f -- "$status_json" "$graph_json"
        exit 0
      fi
    fi
  fi

  if [ "$elapsed" -eq "$timeout_seconds" ]; then
    break
  fi

  sleep "$interval_seconds"
  elapsed=$((elapsed + interval_seconds))
done

printf '%s\n' "Local ingest smoke failed: expected output files were not created before timeout." >&2
printf 'Missing or late: %s\n' "$manifest_output" >&2
printf 'Missing or late: %s\n' "$checksum_output" >&2
printf '%s\n' "Expected ingest status plus a workflow graph with at least four command nodes." >&2
exit 1
