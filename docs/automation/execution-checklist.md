# Execution Checklist

Use this checklist before an agent starts and before it reports completion.

## Before Starting

- [ ] Read `AGENTS.md`.
- [ ] Read `docs/automation/README.md`.
- [ ] Read `docs/automation/github-projects.md`.
- [ ] Identify ownership lane from `docs/automation/roles.md`.
- [ ] Identify linked GitHub issue, user stories, and milestone.
- [ ] Check the linked GitHub issue when remote context is needed.
- [ ] Identify target files.
- [ ] Identify validation command.
- [ ] Check `.worktrees/state/*.md` and `git worktree list` for overlapping work.
- [ ] Confirm no other parallel task owns the same target files.

## During Work

- [ ] Keep edits inside target files.
- [ ] Escalate before dependency, cloud, secret, or out-of-scope work.
- [ ] Follow BDD/TDD for behavior changes.
- [ ] Update GitHub issue or simple Project status only when tracker state changes.
- [ ] Update task, story, milestone, status, and bug docs when durable repo context changes.
- [ ] Record validation evidence.

## Before Reporting Completion

- [ ] Run the cheapest relevant validation.
- [ ] Run stronger validation if the change touches runtime, contracts, infra, or tooling.
- [ ] Run `git diff --check`.
- [ ] Run lightweight GitHub tracker validation when tracker state changed.
- [ ] Update required docs.
- [ ] Prepare handoff using `docs/automation/agent-handoff.md`.
- [ ] If PR creation is authorized, complete `docs/automation/pr-checklist.md`.
- [ ] Report any validation not run and why.
