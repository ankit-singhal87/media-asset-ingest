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
  hierarchy        Print epic sub-issue hierarchy.
  active           Print in-progress project items.

Environment overrides:
  GITHUB_PROJECT_OWNER
  GITHUB_PROJECT_REPO
  GITHUB_PROJECT_NUMBER

Write operations are intentionally not wrapped here. Agents should use gh
directly after reading docs/automation/github-projects.md and after explicit
authorization for remote tracker changes.
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
  -h|--help|help|"")
    usage
    ;;
  *)
    printf 'Unknown command: %s\n\n' "$1" >&2
    usage >&2
    exit 2
    ;;
esac
