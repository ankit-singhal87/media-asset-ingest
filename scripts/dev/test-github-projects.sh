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
test_body=${GH_TEST_BODY:-}

case "$args" in
  *"project item-list 2 --owner ankit-singhal87 --limit 200 --format json"*"missingFields"*)
    printf '%s\n' "#99 TASK-X: Missing fields missing: Type, Validation"
    ;;
  *"issue list --repo ankit-singhal87/media-asset-ingest --state all --limit 200 --json number,title,body"*"Relationship metadata"*)
    printf '%s\n' "#98 TASK-Y: Duplicated relationship metadata"
    ;;
  "pr create --repo ankit-singhal87/media-asset-ingest --base main --head TASK-2-1-create-dotnet-solution --title TASK-2-1: Create .NET solution skeleton --body-file $test_body")
    printf '%s\n' "https://github.com/ankit-singhal87/media-asset-ingest/pull/30"
    ;;
  "project view 2 --owner ankit-singhal87 --format json --jq .id")
    printf '%s\n' "PROJECT_ID"
    ;;
  *"project item-list 2 --owner ankit-singhal87"*".content.number == 30"*)
    printf '%s\n' "ITEM_30"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Status\""*".options[]"*".name == \"In Progress\""*)
    printf '%s\n' "STATUS_IN_PROGRESS"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Status\""*".options[]"*".name == \"PR Open\""*)
    printf '%s\n' "PR_OPEN"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Type\""*".options[]"*".name == \"Task\""*)
    printf '%s\n' "TYPE_TASK"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Lane\""*".options[]"*".name == \"Forge\""*)
    printf '%s\n' "LANE_FORGE"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Status\""*".id")
    printf '%s\n' "STATUS_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Type\""*".id")
    printf '%s\n' "TYPE_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Lane\""*".id")
    printf '%s\n' "LANE_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Worktree / Branch\""*".id")
    printf '%s\n' "WORKTREE_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Target Files\""*".id")
    printf '%s\n' "TARGET_FILES_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"Validation\""*".id")
    printf '%s\n' "VALIDATION_FIELD"
    ;;
  *"project field-list 2 --owner ankit-singhal87"*".name == \"PR\""*".id")
    printf '%s\n' "PR_FIELD"
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id STATUS_FIELD --single-select-option-id STATUS_IN_PROGRESS")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id TYPE_FIELD --single-select-option-id TYPE_TASK")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id LANE_FIELD --single-select-option-id LANE_FORGE")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id STATUS_FIELD --single-select-option-id PR_OPEN")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id WORKTREE_FIELD --text TASK-2-1 / .worktrees/TASK-2-1")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id TARGET_FILES_FIELD --text src; tests")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id VALIDATION_FIELD --text make validate")
    ;;
  "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id PR_FIELD --text https://github.com/ankit-singhal87/media-asset-ingest/pull/30")
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

body_file="$tmp_dir/pr-body.md"
printf '%s\n' "Summary" >"$body_file"
active_worktrees_file="$tmp_dir/active-worktrees.md"
cat >"$active_worktrees_file" <<'ACTIVE_WORKTREES'
| Worktree | Branch | GitHub item | Stories | Lane | Target files | Status | PR |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `.worktrees/TASK-2-1-create-dotnet-solution` | `TASK-2-1-create-dotnet-solution` | #30 | USER-STORY-16 | Forge | `src` | Ready For PR | Not opened |
ACTIVE_WORKTREES

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh set-status 30 "In Progress"

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh set-text 30 "Worktree / Branch" "TASK-2-1 / .worktrees/TASK-2-1"

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh set-task-fields \
    30 Forge "In Progress" "TASK-2-1 / .worktrees/TASK-2-1" "src; tests" "make validate"

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh add-sub-issue 26 30 >/dev/null

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh add-blocked-by 31 30 >/dev/null

if PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh audit-fields >/dev/null 2>"$tmp_dir/audit.err"; then
  printf '%s\n' "Expected audit-fields to fail for missing fields." >&2
  exit 1
fi

grep -F "#99 TASK-X: Missing fields missing: Type, Validation" "$tmp_dir/audit.err" >/dev/null

if PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" \
  sh scripts/dev/github-projects.sh lint-issue-bodies >/dev/null 2>"$tmp_dir/lint.err"; then
  printf '%s\n' "Expected lint-issue-bodies to fail for duplicated relationship metadata." >&2
  exit 1
fi

grep -F "#98 TASK-Y: Duplicated relationship metadata" "$tmp_dir/lint.err" >/dev/null

PATH="$fake_bin:$PATH" GH_TEST_LOG="$log_file" GH_TEST_BODY="$body_file" \
  sh scripts/dev/github-projects.sh open-pr \
    30 \
    TASK-2-1-create-dotnet-solution \
    "TASK-2-1: Create .NET solution skeleton" \
    "$body_file" \
    "$active_worktrees_file" >/dev/null

grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id STATUS_FIELD --single-select-option-id STATUS_IN_PROGRESS" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id TYPE_FIELD --single-select-option-id TYPE_TASK" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id LANE_FIELD --single-select-option-id LANE_FORGE" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id WORKTREE_FIELD --text TASK-2-1 / .worktrees/TASK-2-1" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id TARGET_FILES_FIELD --text src; tests" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id VALIDATION_FIELD --text make validate" "$log_file" >/dev/null
grep -F "repos/ankit-singhal87/media-asset-ingest/issues/26/sub_issues" "$log_file" >/dev/null
grep -F "sub_issue_id=4372284132" "$log_file" >/dev/null
grep -F "repos/ankit-singhal87/media-asset-ingest/issues/31/dependencies/blocked_by" "$log_file" >/dev/null
grep -F "issue_id=4372284132" "$log_file" >/dev/null
grep -F "pr create --repo ankit-singhal87/media-asset-ingest --base main --head TASK-2-1-create-dotnet-solution" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id STATUS_FIELD --single-select-option-id PR_OPEN" "$log_file" >/dev/null
grep -F "project item-edit --project-id PROJECT_ID --id ITEM_30 --field-id PR_FIELD --text https://github.com/ankit-singhal87/media-asset-ingest/pull/30" "$log_file" >/dev/null
grep -F "| \`.worktrees/TASK-2-1-create-dotnet-solution\` | \`TASK-2-1-create-dotnet-solution\` | #30 | USER-STORY-16 | Forge | \`src\` | PR Open | https://github.com/ankit-singhal87/media-asset-ingest/pull/30 |" "$active_worktrees_file" >/dev/null

printf '%s\n' "GitHub Projects helper tests passed."
