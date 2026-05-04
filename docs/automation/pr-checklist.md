# Pull Request Checklist

Use this checklist before creating or merging a pull request.

## Authorization

Agents may create a pull request automatically only when the task, plan, or user
explicitly authorizes automatic PR creation.

Without that authorization, agents must stop after validation and report the
branch/worktree as ready for PR.

Automatic PR creation still requires all checks in this file.

## Required

- [ ] Branch is up to date with target branch or conflict-free.
- [ ] Branch name, commit subject, and PR title follow `docs/automation/git-conventions.md`.
- [ ] `make docs-fix` was run when docs formatting/link checks reported fixable issues.
- [ ] `make validate` passes.
- [ ] `git diff --check` passes.
- [ ] GitHub issue state or simple Project status is updated when tracker state changed.
- [ ] Lightweight GitHub tracker validation runs when issues or board status changed.
- [ ] Product docs, status, and work log are updated when durable repo context changes.
- [ ] Local task and bug mirrors are updated when local task or bug files change.
- [ ] ADRs are updated when architecture decisions change.
- [ ] Quickstart and tooling docs are updated when commands or setup change.
- [ ] Optional host tool warnings are not treated as failures for Docker-first workflows.
- [ ] No secrets, `.env`, Terraform state, kubeconfigs, or cloud credentials are committed.
- [ ] PR description includes summary, validation, risk, and follow-up notes.

## PR Creation Command

Prefer the GitHub plugin after validation and authorization. Use `gh pr create`
only when the plugin cannot cover the PR operation.

The PR body must include:

- summary
- linked tasks, stories, and bugs
- validation commands and outcomes
- risk
- follow-up notes

After creating the PR, update the issue or simple Project status only when that
status is meaningful for visibility.

## Merge Notes

- CI may be disabled for this repository.
- Cloud validation requires explicit approval.
- Squash merge is preferred for documentation and planning branches.
