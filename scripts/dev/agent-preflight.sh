#!/usr/bin/env bash
set -u

repo_path=$(pwd)
git_cmd() {
  git -c "safe.directory=$repo_path" -c core.filemode=false "$@"
}
common_git_dir=$(git_cmd rev-parse --path-format=absolute --git-common-dir 2>/dev/null || true)
if [ -n "$common_git_dir" ]; then
  state_dir="$(dirname "$common_git_dir")/.worktrees/state"
else
  state_dir=".worktrees/state"
fi

printf '%s\n' "Repository:"
git_cmd status --short --branch

printf '\n%s\n' "Branch:"
branch=$(git_cmd branch --show-current 2>/dev/null || true)
if [ -n "$branch" ]; then
  printf '%s\n' "$branch"
else
  printf '%s\n' "(detached or unavailable)"
fi

printf '\n%s\n' "Staged paths:"
staged=$(git_cmd diff --cached --name-only 2>/dev/null || true)
if [ -n "$staged" ]; then
  printf '%s\n' "$staged"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "Unstaged tracked paths:"
unstaged=$(git_cmd diff --name-only 2>/dev/null || true)
if [ -n "$unstaged" ]; then
  printf '%s\n' "$unstaged"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "Changed paths with filemode ignored:"
changed=$(git_cmd diff --name-only HEAD 2>/dev/null || true)
if [ -n "$changed" ]; then
  printf '%s\n' "$changed"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "Worktrees:"
git_cmd worktree list

printf '\n%s\n' "Active worktree state records:"
state_records=$(find "$state_dir" -maxdepth 1 -type f -name '*.md' -print 2>/dev/null | sort || true)
if [ -n "$state_records" ]; then
  printf '%s\n' "$state_records"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "Process reminders:"
printf '%s\n' "- Select a change mode from docs/automation/execution-checklist.md."
printf '%s\n' "- Local commits are allowed after validation; keep them small and scoped."
printf '%s\n' "- Push and PR creation require explicit authorization."
printf '%s\n' "- Stage only intended PR scope; report staged and unstaged paths separately."
printf '%s\n' "- Use subagents for authorized independent lanes and close them after integration."
printf '%s\n' "- Checkpoint before long logs, broad diffs, or compaction risk."

printf '\n%s\n' "Required tools:"
sh scripts/dev/check-tools.sh

printf '\n%s\n' "Canonical validation:"
printf '%s\n' "- make docs-check"
printf '%s\n' "- make scripts-check"
printf '%s\n' "- make test-dotnet"
printf '%s\n' "- make test-ui"
printf '%s\n' "- make validate"
printf '%s\n' "- make validate-summary"
printf '%s\n' "- make pr-readiness-check"

printf '\n%s\n' "Focused .NET validation:"
printf '%s\n' "- make test-dotnet-foundation"
printf '%s\n' "- make test-dotnet-contracts"
printf '%s\n' "- make test-dotnet-watcher"
printf '%s\n' "- make test-dotnet-persistence"
printf '%s\n' "- make test-dotnet-outbox"
printf '%s\n' "- make test-dotnet-workflow"
printf '%s\n' "- make test-dotnet-observability"
printf '%s\n' "- make test-dotnet-command-runner"
