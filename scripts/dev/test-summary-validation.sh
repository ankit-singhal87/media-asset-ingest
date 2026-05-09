#!/bin/sh
set -eu

tmp_dir=$(mktemp -d)
trap 'rm -rf "$tmp_dir"' EXIT

fake_bin="$tmp_dir/bin"
mkdir -p "$fake_bin"

cat >"$fake_bin/fake-validation" <<'FAKE_VALIDATION'
#!/bin/sh
set -eu

printf '%s\n' "Starting validation"
printf '%s\n' "PASS FoundationSmoke"
printf '%s\n' "FAIL MediaIngest.Tests.CommandRunnerRoutesHeavyWork"
printf '%s\n' "Error Message: expected heavy runner route"
printf '%s\n' "Failed MediaIngest.Tests.ManifestRequiredBeforePackageStart"
printf '%s\n' "Final line that should remain in the log only"
exit 23
FAKE_VALIDATION

cat >"$fake_bin/fake-success" <<'FAKE_SUCCESS'
#!/bin/sh
set -eu

printf '%s\n' "Starting validation"
printf '%s\n' "Automation helper tests passed."
printf '%s\n' "Test Run Successful."
exit 0
FAKE_SUCCESS

chmod +x "$fake_bin/fake-validation" "$fake_bin/fake-success"

failure_summary="$tmp_dir/failure-summary.txt"
if PATH="$fake_bin:$PATH" sh scripts/dev/validation-summary.sh fake-validation >"$failure_summary" 2>&1; then
  printf '%s\n' "Expected summary wrapper to return the wrapped command exit code." >&2
  exit 1
fi

grep -F "command: fake-validation" "$failure_summary" >/dev/null
grep -F "exit code: 23" "$failure_summary" >/dev/null
grep -F "failing lines:" "$failure_summary" >/dev/null
grep -F "FAIL MediaIngest.Tests.CommandRunnerRoutesHeavyWork" "$failure_summary" >/dev/null
grep -F "Failed MediaIngest.Tests.ManifestRequiredBeforePackageStart" "$failure_summary" >/dev/null
grep -E "^log: /tmp/media-asset-ingest-validation-[^ ]+\\.log$" "$failure_summary" >/dev/null
log_path=$(sed -n 's/^log: //p' "$failure_summary")
test -f "$log_path"
grep -F "Final line that should remain in the log only" "$log_path" >/dev/null

success_summary="$tmp_dir/success-summary.txt"
PATH="$fake_bin:$PATH" sh scripts/dev/validation-summary.sh fake-success >"$success_summary" 2>&1

grep -F "command: fake-success" "$success_summary" >/dev/null
grep -F "exit code: 0" "$success_summary" >/dev/null
grep -F "success lines:" "$success_summary" >/dev/null
grep -F "Automation helper tests passed." "$success_summary" >/dev/null
grep -F "Test Run Successful." "$success_summary" >/dev/null
grep -E "^log: /tmp/media-asset-ingest-validation-[^ ]+\\.log$" "$success_summary" >/dev/null

printf '%s\n' "Summary validation wrapper tests passed."
