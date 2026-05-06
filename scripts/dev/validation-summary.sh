#!/bin/sh
set -u

if [ "$#" -eq 0 ]; then
  printf '%s\n' "Usage: sh scripts/dev/validation-summary.sh <command> [args...]" >&2
  exit 2
fi

log_file=$(mktemp "${TMPDIR:-/tmp}/media-asset-ingest-validation-XXXXXX.log")
command_text=$*

"$@" >"$log_file" 2>&1
exit_code=$?

printf '%s\n' "command: $command_text"
printf '%s\n' "exit code: $exit_code"

if [ "$exit_code" -eq 0 ]; then
  success_lines=$(grep -E \
    "([Tt]ests? passed|[Tt]est [Rr]un [Ss]uccessful|Build succeeded|Validation passed|passed\\.|checks? passed|successful\\.)" \
    "$log_file" | tail -n 20 || true)
  if [ -n "$success_lines" ]; then
    printf '%s\n' "success lines:"
    printf '%s\n' "$success_lines"
  else
    printf '%s\n' "success lines:"
    tail -n 5 "$log_file"
  fi
else
  failing_lines=$(grep -E \
    "(^|[[:space:]])(FAIL|Failed|failed|Error Message:|Assertion|Exception|error:|fatal:)" \
    "$log_file" | tail -n 40 || true)
  if [ -n "$failing_lines" ]; then
    printf '%s\n' "failing lines:"
    printf '%s\n' "$failing_lines"
  else
    printf '%s\n' "failing lines:"
    tail -n 20 "$log_file"
  fi
fi

printf '%s\n' "log: $log_file"
exit "$exit_code"
