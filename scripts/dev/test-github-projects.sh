#!/bin/sh
set -eu

tmp_dir=$(mktemp -d)
trap 'rm -rf "$tmp_dir"' EXIT

log_file="$tmp_dir/gh.log"
fake_bin="$tmp_dir/bin"
mkdir -p "$fake_bin"

cat >"$fake_bin/gh" <<'FAKE_GH'
#!/bin/sh
set -eu

printf '%s\n' "$*" >> "$GH_TEST_LOG"

args=$*

case "$args" in
  "project view 2 --owner ankit-singhal87 --format json --jq .id")
    printf '%s\n' "PROJECT_ID"
    ;;
  *"project item-list 2 --owner ankit-singhal87"*".content.number == 30"*)
    printf '%s\n' "ITEM_30"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Status\""*".options[]"*".name == \"In Progress\""*)
    printf '%s\n' "STATUS_IN_PROGRESS"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Status\""*".id")
    printf '%s\n' "STATUS_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Worktree / Branch\""*".id")
    printf '%s\n' "WORKTREE_FIELD"
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id STATUS_FIELD --single-select-option-id STATUS_IN_PROGRESS")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id WORKTREE_FIELD --text TASK-2-1 / .worktrees/TASK-2-1")
    ;;
  "api repos/ankit-singhal87/media-asset-ingest/issues/30 --jq .id")
    printf '%s\n' "4372284132"
    ;;
  *"api -X POST repos/ankit-singhal87/media-asset-ingest/issues/26/sub_issues"*"sub_issue_id=4372284132"*)
    printf '%s\n' '{"completed":0,"percent_completed":0,"total":1}'
    ;;
  *"api -X POST repos/ankit-singhal87/media-asset-ingest/issues/31/dependencies/blocked_by"*"issue_id=4372284132"*)
    printf '%s\n' '{"blocked_by":1,"total_blocked_by":1}'
    ;;
  *)
    printf 'Unexpected gh invocation: %s\n' "$args" >&2
    exit 99
    ;;
esac
FAKE_GH

chmod +x "$fake_bin/gh"

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh set-status 30 "In Progress"

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh set-text 30 "Worktree / Branch" "TASK-2-1 / .worktrees/TASK-2-1"

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh add-sub-issue 26 30 >/dev/null

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh add-blocked-by 31 30 >/dev/null

grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id STATUS_FIELD --single-select-option-id STATUS_IN_PROGRESS" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id WORKTREE_FIELD --text TASK-2-1 / .worktrees/TASK-2-1" "$log_file" >/dev/null
grep -F "repos/ankit-singhal87/media-asset-ingest/issues/26/sub_issues" "$log_file" >/dev/null
grep -F "sub_issue_id=4372284132" "$log_file" >/dev/null
grep -F "repos/ankit-singhal87/media-asset-ingest/issues/31/dependencies/blocked_by" "$log_file" >/dev/null
grep -F "issue_id=4372284132" "$log_file" >/dev/null

printf '%s\n' "GitHub Projects helper tests passed."
