#!/bin/sh
set -eu

OWNER=${GITHUB_PROJECT_OWNER:-ankit-singhal87}
REPO=${GITHUB_PROJECT_REPO:-ankit-singhal87/media-asset-ingest}
PROJECT_NUMBER=${GITHUB_PROJECT_NUMBER:-2}

usage() {
  cat <<'USAGE'
Usage: scripts/dev/github-projects.sh <command>

Commands:
  check-auth       Verify GitHub CLI auth and project access.
  summary          Print project, milestone, issue, and item counts.
  active           Print active story board items.
  hierarchy        Legacy: print epic sub-issue hierarchy.
  audit-fields     Legacy: verify detailed Project fields are populated.
  lint-issue-bodies Legacy: check duplicated relationship metadata.
  open-pr          Legacy: create a PR and update tracker/worktree PR state.
  set-status       Set GitHub Project Status for an issue.
  set-type         Legacy: set GitHub Project Type for an issue.
  set-lane         Legacy: set GitHub Project Lane for an issue.
  set-text         Legacy: set a GitHub Project text field for an issue.
  set-task-fields  Legacy: set common task Project fields.
  add-sub-issue    Legacy: add a native GitHub sub-issue relationship.
  add-blocked-by   Legacy: add a native GitHub blocked-by dependency.

Environment overrides:
  GITHUB_PROJECT_OWNER
  GITHUB_PROJECT_REPO
  GITHUB_PROJECT_NUMBER

Write operations require explicit agent approval before running the command.
Use only summary, active, and simple status movement for normal lightweight
tracking unless a future task explicitly revives detailed Project maintenance.
USAGE
}

require_gh() {
  if ! command -v gh >/dev/null 2>&1; then
    printf '%s\n' "gh is required. Run make check-tools for guidance." >&2
    exit 1
  fi
}

check_auth() {
  gh auth status
  gh project view "$PROJECT_NUMBER" --owner "$OWNER" --format json --jq '.title'
}

summary() {
  printf 'project: '
  gh project view "$PROJECT_NUMBER" --owner "$OWNER" --format json --jq '.title + " (" + .url + ")"'
  printf 'project_items: '
  gh project item-list "$PROJECT_NUMBER" --owner "$OWNER" --limit 200 --format json --jq '.items | length'
  printf 'issues: '
  gh issue list --repo "$REPO" --state all --limit 200 --json number --jq 'length'
  printf 'milestones: '
  gh api "repos/$REPO/milestones" --paginate --jq 'length'
}

hierarchy() {
  gh issue list --repo "$REPO" --state all --label type:epic --limit 100 \
    --json number,title --jq '.[] | [.number, .title] | @tsv' |
  while IFS="$(printf '\t')" read -r number title; do
    printf '#%s %s\n' "$number" "$title"
    gh api "repos/$REPO/issues/$number/sub_issues" \
      -H 'X-GitHub-Api-Version: 2026-03-10' \
      --jq '.[] | "  - #\(.number) \(.title)"'
  done
}

active() {
  gh project item-list "$PROJECT_NUMBER" --owner "$OWNER" --limit 200 --format json \
    --jq '.items[] | select(.status == "In Progress") | "#\(.content.number) \(.title)"'
}

audit_fields() {
  missing=$(
    gh project item-list "$PROJECT_NUMBER" --owner "$OWNER" --limit 200 --format json \
      --jq '
        def required:
          if .type == "Task" then
            {
              "Type": .type,
              "Lane": .lane,
              "Status": .status,
              "Worktree / Branch": .["worktree / Branch"],
              "Target Files": .["target Files"],
              "Validation": .validation
            }
          else
            {
              "Type": .type,
              "Lane": .lane,
              "Status": .status
            }
          end;
        .items[]
        | (required | to_entries | map(select((.value // "") == "")) | map(.key)) as $missingFields
        | select($missingFields | length > 0)
        | "#\(.content.number) \(.title) missing: \($missingFields | join(", "))"
      '
  )

  if [ -n "$missing" ]; then
    printf '%s\n' "$missing" >&2
    exit 1
  fi

  printf '%s\n' "GitHub Project field audit passed."
}

lint_issue_bodies() {
  violations=$(
    gh issue list --repo "$REPO" --state all --limit 200 --json number,title,body \
      --jq '
        .[]
        | select((.body // "") | test("(?im)^##[[:space:]]+(Parent Epic|Dependencies|Related Epic|Related Milestone|Related Story|Related Task|Related Milestone)\\b"))
        | "#\(.number) \(.title): Relationship metadata belongs in GitHub native fields, not issue body sections."
      '
  )

  if [ -n "$violations" ]; then
    printf '%s\n' "$violations" >&2
    exit 1
  fi

  printf '%s\n' "GitHub issue body relationship lint passed."
}

project_id() {
  if [ -z "${PROJECT_ID_CACHE:-}" ]; then
    PROJECT_ID_CACHE=$(gh project view "$PROJECT_NUMBER" --owner "$OWNER" --format json --jq '.id')
  fi
  printf '%s\n' "$PROJECT_ID_CACHE"
}

project_item_id_for_issue() {
  issue_number=$1
  cache_var="PROJECT_ITEM_ID_${issue_number}"
  eval "cached=\${$cache_var:-}"
  if [ -z "$cached" ]; then
    cached=$(
      gh project item-list "$PROJECT_NUMBER" --owner "$OWNER" --limit 200 --format json \
        --jq ".items[] | select(.content.number == $issue_number) | .id"
    )
    eval "$cache_var=\$cached"
  fi
  printf '%s\n' "$cached"
}

project_field_id() {
  field_name=$1
  case "$field_name" in
    Type) cache_var=PROJECT_FIELD_TYPE ;;
    Lane) cache_var=PROJECT_FIELD_LANE ;;
    Status) cache_var=PROJECT_FIELD_STATUS ;;
    "Worktree / Branch") cache_var=PROJECT_FIELD_WORKTREE ;;
    "Target Files") cache_var=PROJECT_FIELD_TARGET_FILES ;;
    Validation) cache_var=PROJECT_FIELD_VALIDATION ;;
    PR) cache_var=PROJECT_FIELD_PR ;;
    *)
      printf 'Unsupported cached Project field: %s\n' "$field_name" >&2
      exit 2
      ;;
  esac

  eval "cached=\${$cache_var:-}"
  if [ -z "$cached" ]; then
    cached=$(
      gh project field-list "$PROJECT_NUMBER" --owner "$OWNER" --format json \
        --jq ".fields[] | select(.name == \"$field_name\") | .id"
    )
    eval "$cache_var=\$cached"
  fi
  printf '%s\n' "$cached"
}

project_option_id() {
  field_name=$1
  option_name=$2
  case "$field_name:$option_name" in
    Type:Task) cache_var=PROJECT_OPTION_TYPE_TASK ;;
    Status:"In Progress") cache_var=PROJECT_OPTION_STATUS_IN_PROGRESS ;;
    Status:"PR Open") cache_var=PROJECT_OPTION_STATUS_PR_OPEN ;;
    Lane:Atlas) cache_var=PROJECT_OPTION_LANE_ATLAS ;;
    Lane:Mount) cache_var=PROJECT_OPTION_LANE_MOUNT ;;
    Lane:Pulse) cache_var=PROJECT_OPTION_LANE_PULSE ;;
    Lane:Courier) cache_var=PROJECT_OPTION_LANE_COURIER ;;
    Lane:Vault) cache_var=PROJECT_OPTION_LANE_VAULT ;;
    Lane:Essence) cache_var=PROJECT_OPTION_LANE_ESSENCE ;;
    Lane:Beacon) cache_var=PROJECT_OPTION_LANE_BEACON ;;
    Lane:Canvas) cache_var=PROJECT_OPTION_LANE_CANVAS ;;
    Lane:Forge) cache_var=PROJECT_OPTION_LANE_FORGE ;;
    Lane:Gauge) cache_var=PROJECT_OPTION_LANE_GAUGE ;;
    Lane:Shield) cache_var=PROJECT_OPTION_LANE_SHIELD ;;
    *)
      printf 'Unsupported cached Project option: %s=%s\n' "$field_name" "$option_name" >&2
      exit 2
      ;;
  esac

  eval "cached=\${$cache_var:-}"
  if [ -z "$cached" ]; then
    cached=$(
      gh project field-list "$PROJECT_NUMBER" --owner "$OWNER" --format json \
        --jq ".fields[] | select(.name == \"$field_name\") | .options[] | select(.name == \"$option_name\") | .id"
    )
    eval "$cache_var=\$cached"
  fi
  printf '%s\n' "$cached"
}

require_arg() {
  value=$1
  name=$2
  if [ -z "$value" ]; then
    printf 'Missing required argument: %s\n\n' "$name" >&2
    usage >&2
    exit 2
  fi
}

set_single_select_field() {
  issue_number=$1
  field_name=$2
  option_name=$3

  project=$(project_id)
  item=$(project_item_id_for_issue "$issue_number")
  field=$(project_field_id "$field_name")
  option=$(project_option_id "$field_name" "$option_name")

  if [ -z "$item" ] || [ -z "$field" ] || [ -z "$option" ]; then
    printf 'Unable to resolve Project item, field, or option for issue #%s.\n' "$issue_number" >&2
    exit 1
  fi

  gh project item-edit \
    --project-id "$project" \
    --id "$item" \
    --field-id "$field" \
    --single-select-option-id "$option"
}

set_text_field() {
  issue_number=$1
  field_name=$2
  text_value=$3

  project=$(project_id)
  item=$(project_item_id_for_issue "$issue_number")
  field=$(project_field_id "$field_name")

  if [ -z "$item" ] || [ -z "$field" ]; then
    printf 'Unable to resolve Project item or field for issue #%s.\n' "$issue_number" >&2
    exit 1
  fi

  gh project item-edit \
    --project-id "$project" \
    --id "$item" \
    --field-id "$field" \
    --text "$text_value"
}

set_task_fields() {
  issue_number=$1
  lane=$2
  status=$3
  worktree_branch=$4
  target_files=$5
  validation=$6

  set_single_select_field "$issue_number" "Type" "Task"
  set_single_select_field "$issue_number" "Lane" "$lane"
  set_single_select_field "$issue_number" "Status" "$status"
  set_text_field "$issue_number" "Worktree / Branch" "$worktree_branch"
  set_text_field "$issue_number" "Target Files" "$target_files"
  set_text_field "$issue_number" "Validation" "$validation"
}

mark_active_worktree_pr_open() {
  branch=$1
  pr_url=$2
  active_worktrees_file=$3
  tmp_file="${active_worktrees_file}.tmp"

  awk -v branch="| \`$branch\` |" -v pr_url="$pr_url" '
    index($0, branch) {
      sub(/\| Ready For PR \| [^|]* \|$/, "| PR Open | " pr_url " |")
      sub(/\| Active \| [^|]* \|$/, "| PR Open | " pr_url " |")
    }
    { print }
  ' "$active_worktrees_file" >"$tmp_file"
  mv "$tmp_file" "$active_worktrees_file"
}

open_pr() {
  issue_number=$1
  head_branch=$2
  title=$3
  body_file=$4
  active_worktrees_file=$5

  if [ ! -f "$body_file" ]; then
    printf 'PR body file not found: %s\n' "$body_file" >&2
    exit 1
  fi

  if [ ! -f "$active_worktrees_file" ]; then
    printf 'Active worktrees file not found: %s\n' "$active_worktrees_file" >&2
    exit 1
  fi

  pr_url=$(
    gh pr create \
      --repo "$REPO" \
      --base main \
      --head "$head_branch" \
      --title "$title" \
      --body-file "$body_file"
  )

  set_single_select_field "$issue_number" "Status" "PR Open"
  set_text_field "$issue_number" "PR" "$pr_url"
  mark_active_worktree_pr_open "$head_branch" "$pr_url" "$active_worktrees_file"

  printf '%s\n' "$pr_url"
}

rest_issue_id() {
  issue_number=$1
  gh api "repos/$REPO/issues/$issue_number" --jq '.id'
}

add_sub_issue() {
  parent_number=$1
  child_number=$2
  child_id=$(rest_issue_id "$child_number")

  gh api -X POST "repos/$REPO/issues/$parent_number/sub_issues" \
    -H 'X-GitHub-Api-Version: 2026-03-10' \
    -F "sub_issue_id=$child_id" \
    --jq '.sub_issues_summary'
}

add_blocked_by() {
  blocked_issue_number=$1
  blocking_issue_number=$2
  blocking_id=$(rest_issue_id "$blocking_issue_number")

  gh api -X POST "repos/$REPO/issues/$blocked_issue_number/dependencies/blocked_by" \
    -H 'X-GitHub-Api-Version: 2026-03-10' \
    -F "issue_id=$blocking_id" \
    --jq '.issue_dependencies_summary'
}

require_gh

case "${1:-}" in
  check-auth)
    check_auth
    ;;
  summary)
    summary
    ;;
  hierarchy)
    hierarchy
    ;;
  active)
    active
    ;;
  audit-fields)
    audit_fields
    ;;
  lint-issue-bodies)
    lint_issue_bodies
    ;;
  open-pr)
    require_arg "${2:-}" "issue-number"
    require_arg "${3:-}" "head-branch"
    require_arg "${4:-}" "title"
    require_arg "${5:-}" "body-file"
    require_arg "${6:-}" "active-worktrees-file"
    open_pr "$2" "$3" "$4" "$5" "$6"
    ;;
  set-status)
    require_arg "${2:-}" "issue-number"
    require_arg "${3:-}" "status"
    set_single_select_field "$2" "Status" "$3"
    ;;
  set-type)
    require_arg "${2:-}" "issue-number"
    require_arg "${3:-}" "type"
    set_single_select_field "$2" "Type" "$3"
    ;;
  set-lane)
    require_arg "${2:-}" "issue-number"
    require_arg "${3:-}" "lane"
    set_single_select_field "$2" "Lane" "$3"
    ;;
  set-text)
    require_arg "${2:-}" "issue-number"
    require_arg "${3:-}" "field-name"
    require_arg "${4:-}" "text"
    set_text_field "$2" "$3" "$4"
    ;;
  set-task-fields)
    require_arg "${2:-}" "issue-number"
    require_arg "${3:-}" "lane"
    require_arg "${4:-}" "status"
    require_arg "${5:-}" "worktree-branch"
    require_arg "${6:-}" "target-files"
    require_arg "${7:-}" "validation"
    set_task_fields "$2" "$3" "$4" "$5" "$6" "$7"
    ;;
  add-sub-issue)
    require_arg "${2:-}" "parent-issue-number"
    require_arg "${3:-}" "child-issue-number"
    add_sub_issue "$2" "$3"
    ;;
  add-blocked-by)
    require_arg "${2:-}" "blocked-issue-number"
    require_arg "${3:-}" "blocking-issue-number"
    add_blocked_by "$2" "$3"
    ;;
  -h|--help|help|"")
    usage
    ;;
  *)
    printf 'Unknown command: %s\n\n' "$1" >&2
    usage >&2
    exit 2
    ;;
esac
