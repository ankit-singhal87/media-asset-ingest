#!/usr/bin/env bash
set -u

printf '%s\n' "Repository:"
git status --short --branch

printf '\n%s\n' "Worktrees:"
git worktree list

printf '\n%s\n' "Required tools:"
sh scripts/dev/check-tools.sh

printf '\n%s\n' "Canonical validation:"
printf '%s\n' "- make docs-check"
printf '%s\n' "- make scripts-check"
printf '%s\n' "- make test-dotnet"
printf '%s\n' "- make validate"

printf '\n%s\n' "Focused .NET validation:"
printf '%s\n' "- make test-dotnet-foundation"
printf '%s\n' "- make test-dotnet-contracts"
printf '%s\n' "- make test-dotnet-watcher"
printf '%s\n' "- make test-dotnet-persistence"
printf '%s\n' "- make test-dotnet-outbox"
printf '%s\n' "- make test-dotnet-workflow"
printf '%s\n' "- make test-dotnet-observability"
