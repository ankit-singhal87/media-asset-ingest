# Execution Checklist

Use this checklist before an agent starts and before it reports completion.

## Before Starting

- [ ] Read `AGENTS.md`.
- [ ] Read `docs/automation/README.md`.
- [ ] Read `docs/automation/github-projects.md`.
- [ ] Identify ownership lane from `docs/automation/roles.md`.
- [ ] Identify linked GitHub issue, user stories, and milestone.
- [ ] Check GitHub parent/sub-issue and dependency relationships.
- [ ] Identify target files.
- [ ] Identify validation command.
- [ ] Check `docs/plans/active-worktrees.md` for overlapping work.
- [ ] Confirm no other parallel task owns the same target files.

## During Work

- [ ] Keep edits inside target files.
- [ ] Escalate before dependency, cloud, secret, or out-of-scope work.
- [ ] Follow BDD/TDD for behavior changes.
- [ ] Update GitHub Projects for issue, status, dependency, PR, or tracker changes.
- [ ] Update task, story, milestone, status, and bug docs when durable repo context changes.
- [ ] Record validation evidence.

## Before Reporting Completion

- [ ] Run the cheapest relevant validation.
- [ ] Run stronger validation if the change touches runtime, contracts, infra, or tooling.
- [ ] Run `git diff --check`.
- [ ] Run relevant GitHub tracker validation from `docs/automation/validation.md`.
- [ ] Update required docs.
- [ ] Prepare handoff using `docs/automation/agent-handoff.md`.
- [ ] If PR creation is authorized, complete `docs/automation/pr-checklist.md`.
- [ ] Report any validation not run and why.
