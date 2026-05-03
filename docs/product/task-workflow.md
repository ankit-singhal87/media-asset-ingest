# Task Workflow

## Required Flow

Each implementation task should follow:

1. Confirm scope and ownership lane.
2. Read `AGENTS.md` and `docs/automation/README.md`.
3. Read relevant architecture, ADR, product, and standards docs.
4. Write or identify a BDD scenario for user-visible behavior.
5. Write a failing test before production code.
6. Implement the smallest change that passes.
7. Refactor while tests stay green.
8. Run the validation command from `docs/automation/validation.md`.
9. Update impacted docs.
10. Report validation evidence.

## Required Documentation Updates

Before a task is complete, update the relevant files:

- `docs/status/current-status.md` when phase, focus, next action, or milestone state changes.
- `docs/status/work-log.md` for meaningful completed work or bug fixes.
- `docs/product/backlog.md` when work is added, completed, split, or deferred.
- `docs/product/milestones.md` when milestone scope or progress changes.
- `docs/product/user-stories.md` when acceptance meaning changes.
- `README.md` quickstart when onboarding, setup, commands, or validation changes.
- `docs/adr` when an architectural decision changes.
- `docs/standards/tooling.md` when a required development tool changes.
- `scripts/dev/check-tools.sh` when a required tool must be detected.
- `scripts/dev/install-tools.sh` when a required tool should be installable.
- `docs/automation/commands.md` when a canonical command is added or changed.
- `docs/automation/validation.md` when validation expectations change.

## Bug Fixes

Bug fixes must include:

- the observed behavior
- the expected behavior
- a failing regression test first, unless the user approves an exception
- the fix
- validation evidence
- any status or work-log update
