# Definition Of Done

## Story Done

- Acceptance themes are implemented or explicitly deferred.
- Tests or validation cover the story behavior.
- Linked GitHub issues are completed or deferred with rationale when they exist.
- GitHub Project status reflects the final story state when the story is on the board.
- `docs/product/user-stories.md` status is updated.
- `docs/product/milestones.md` mapping remains accurate.
- No open bugs block the story.

## Task Done

- Work stayed inside declared target files or escalation was approved.
- BDD scenario is confirmed or updated.
- TDD RED/GREEN evidence is recorded, or exception is documented.
- Minimal validation passes.
- Required docs are updated.
- Handoff result is prepared.
- GitHub issue or simple Project status is updated when remote tracking is used.
- `docs/plans/task-index.md` is updated when local task files change.

## Bug Fix Done

- Bug file exists under `docs/bugs`.
- Observed and expected behavior are documented.
- Regression test fails before the fix and passes after, unless exception is approved.
- Linked stories/tasks are updated if meaning changes.
- GitHub bug issue and simple Project status are updated when remote tracking is used.
- `docs/bugs/index.md` is updated when local bug files change.
- Validation evidence is recorded.

## Milestone Done

- All milestone tasks are completed or deferred.
- Linked stories reflect current status.
- GitHub milestone/issues and simple Project status reflect current status when
  remote tracking is used.
- Validation evidence is recorded.
- Status/work log are updated.
- Known bugs are filed and non-blocking.

## Architecture Change Done

- ADR is added or updated when a durable decision changes.
- Architecture docs reflect the decision.
- Product stories and plans are updated if scope changes.
- GitHub issues and simple Project status are updated if roadmap scope changes.
- Validation confirms docs have no unfinished placeholders.

## Tooling Change Done

- `README.md` quickstart is updated if onboarding changes.
- `docs/standards/tooling.md` is updated.
- `scripts/dev/check-tools.sh` is updated for required tool detection.
- `scripts/dev/install-tools.sh` is updated when install guidance changes.
- `docs/automation/commands.md` and `validation.md` are updated.
- `make validate` passes.

## GitHub Tracker Change Done

- Issue state and simple Project status are updated when needed.
- Issue bodies stay focused on problem, outcome, and implementation notes.
- `make github-project-summary` passes.
- `make github-project-active` reflects the intended active board state.
