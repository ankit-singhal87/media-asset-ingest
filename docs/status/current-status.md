# Current Status

## Project State

- Phase: planning and repository foundation.
- Current branch: `USER-STORY-16-link-and-commit-standards`.
- Current focus: durable project documentation, automation guidance, standards,
  and toolchain bootstrap.

## Completed

- Initial architecture direction selected.
- Azure Service Bus selected for messaging.
- PostgreSQL selected for business state and outbox.
- Dapr Workflow selected for orchestration.
- Message-centric orchestration selected.
- Automation docs and ownership lanes created.
- Product stories and milestones created.
- BDD/TDD, DDD, and tooling standards created.
- Linux tool check/install scripts created.
- Makefile and npm validation entrypoints created.
- README quickstart added with Linux-first and Docker-first guidance.
- Milestones and user stories assigned stable IDs.
- Planning and bug folder structure created.
- Agent execution templates, indexes, checklists, handoff, parallelization, and
  conflict guidance created.
- Active worktree tracking and PR authorization rules created.
- GitHub Projects roadmap created with milestone epics and numbered user-story
  issues.
- GitHub issue hierarchy, dependencies, issue bodies, and Project fields
  refined.
- Read-only GitHub tracker helper commands added for agent verification.
- Local task, bug, worktree, checklist, and definition-of-done docs aligned with
  the GitHub Projects tracker model.
- Markdown link validation, safe docs fix commands, and Git branch/commit/PR
  naming conventions are being added under USER-STORY-16.

## Ready For Review

- Link and commit standards cleanup is validated and ready for PR when
  authorized.

## Next

- Create PR for `USER-STORY-16-link-and-commit-standards` after user approval.

## Known Agent Execution Gaps

- GitHub Project write helpers are intentionally manual `gh` calls; agents have
  read-only Make helpers but no safe scripted creator/updater for task and bug
  issues yet.
- Project custom fields are populated for current issues, but helper scripts do
  not verify required field completeness across all items.
- Task and bug templates now reference GitHub issues, but no implementation task
  issues have been created under story issues yet.
- There is no automated check that issue bodies avoid duplicated relationship
  metadata.
- There is no standardized PR creation helper that updates GitHub Project fields
  and `docs/plans/active-worktrees.md` together.

## Update Rule

Agents must update this file when a task changes the project phase, active
milestone, completed work, current focus, or next action.
