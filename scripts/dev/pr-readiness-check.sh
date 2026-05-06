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

printf '%s\n' "PR readiness dry check"

printf '\n%s\n' "Branch:"
branch=$(git_cmd branch --show-current 2>/dev/null || true)
if [ -n "$branch" ]; then
  printf '%s\n' "$branch"
else
  printf '%s\n' "(detached or unavailable)"
fi

printf '\n%s\n' "Status:"
git_cmd status --short --branch

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

printf '\n%s\n' "Untracked paths:"
untracked=$(git_cmd ls-files --others --exclude-standard 2>/dev/null || true)
if [ -n "$untracked" ]; then
  printf '%s\n' "$untracked"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "Change summary:"
git_cmd diff --cached --stat
git_cmd diff --stat

printf '\n%s\n' "File mode summary:"
summary=$(git_cmd diff --cached --summary; git_cmd diff --summary)
if [ -n "$summary" ]; then
  printf '%s\n' "$summary"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "State records:"
state_records=$(find "$state_dir" -maxdepth 1 -type f -name '*.md' -print 2>/dev/null | sort || true)
if [ -n "$state_records" ]; then
  printf '%s\n' "$state_records"
else
  printf '%s\n' "(none)"
fi

printf '\n%s\n' "Suggested validation by changed path:"
paths=$(printf '%s\n%s\n%s\n' "$staged" "$unstaged" "$untracked" | sort -u | sed '/^$/d')
if [ -z "$paths" ]; then
  printf '%s\n' "- No tracked content changes detected."
else
  printf '%s\n' "$paths" | while IFS= read -r path; do
    case "$path" in
      Makefile|package.json|scripts/dev/*)
        printf '%s\n' "- $path: make validate"
        ;;
      web/*)
        printf '%s\n' "- $path: make test-ui"
        ;;
      docs/automation/*)
        printf '%s\n' "- $path: make docs-check && git diff --check"
        ;;
      src/MediaIngest.Worker.CommandRunner/*|tests/MediaIngest.Worker.CommandRunner.Tests/*)
        printf '%s\n' "- $path: make test-dotnet-command-runner"
        ;;
      src/*|tests/*)
        printf '%s\n' "- $path: focused make test-dotnet-* target, then make validate before PR"
        ;;
      *)
        printf '%s\n' "- $path: choose validation from docs/automation/validation.md"
        ;;
    esac
  done | sort -u
fi

printf '\n%s\n' "Required before PR:"
printf '%s\n' "- explicit authorization to push and open PR"
printf '%s\n' "- local commits are allowed after validation; prefer small scoped commits"
printf '%s\n' "- intended staged scope only"
printf '%s\n' "- make validate when command docs, scripts, Makefile targets, contracts, or tooling changed"
printf '%s\n' "- git diff --check and git diff --cached --check"
printf '%s\n' "- updated .worktrees/state/<worktree-slug>.md"
printf '%s\n' "- handoff from docs/automation/agent-handoff.md"
