#!/bin/sh
set -eu

tmp_dir=$(mktemp -d)
trap 'rm -rf "$tmp_dir"' EXIT

runtime_plan="$tmp_dir/runtime-plan.txt"
sh scripts/dev/local-compose-check.sh --runtime-smoke --dry-run >"$runtime_plan"

grep -F "verify command-runner-light, command-runner-medium, and command-runner-heavy are running" "$runtime_plan" >/dev/null
grep -F "verify each command-runner log exposes its configured executionClass" "$runtime_plan" >/dev/null
grep -F "query PostgreSQL for dispatched command rows grouped by executionClass" "$runtime_plan" >/dev/null

printf '%s\n' "Local Compose check script tests passed."
