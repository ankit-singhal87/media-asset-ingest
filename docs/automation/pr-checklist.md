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
- [ ] GitHub issue state is updated when tracker state changed.
- [ ] Simple Project status is updated only when story-level board visibility changed.
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

After creating the PR, update the issue state when useful. Update simple Project
status only when story-level board visibility changes.

## PR Readiness Gate

Implementation complete does not automatically mean push or PR creation is
authorized. Local commits are allowed after validation and should be small,
coherent checkpoints.

Before committing, pushing, or opening a PR:

- Confirm the user, task, or plan explicitly authorizes push and PR creation.
- Confirm the branch is up to date with the target branch or conflict-free.
- Review staged and unstaged paths separately and keep the staged set limited to
  the intended local commit or PR scope.
- Run `make validate` when command docs, scripts, Makefile targets, runtime
  contracts, or tooling behavior changed.
- Run `git diff --check` and `git diff --cached --check` when staged changes
  exist.
- Update `.worktrees/state/<worktree-slug>.md` with branch status, validation
  evidence, and whether the PR is not created, open, or merged.
- Run `make pr-readiness-check` and resolve any unexpected staged paths,
  missing state records, or validation reminders.
- Prepare the handoff from `docs/automation/agent-handoff.md`.

Prefer multiple small local commits over one large commit when a branch contains
separable process, tooling, documentation, or implementation checkpoints.

## Merge Notes

- CI may be disabled for this repository.
- Cloud validation requires explicit approval.
- Squash merge is preferred for documentation and planning branches.

## Post-Merge Agent Cleanup

After a PR is merged and the target branch is fast-forwarded locally:

- Remove the completed local worktree with `git worktree remove <path>` once no
  uncommitted work remains there.
- Delete the merged local feature branch after removing its worktree when it is
  no longer needed.
- Delete the ignored `.worktrees/state/<worktree-slug>.md` record for completed
  work so future agents do not treat it as active.
- Prune stale worktree metadata with `git worktree prune` if `git worktree list`
  shows missing or stale entries.
- Leave remote branch deletion to the repository owner or the GitHub PR merge
  settings unless the user explicitly asks for remote branch cleanup.

Report the cleanup result in the `cleanup` block from
`docs/automation/agent-handoff.md`. If cleanup is skipped or blocked, state the
reason and leave the `.worktrees/state/<worktree-slug>.md` record updated rather
than stale.
