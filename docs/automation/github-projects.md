# GitHub Projects

GitHub Projects is the human-facing source of truth for roadmap tracking.
Repo-local planning docs remain the durable product and execution context that
agents read before work.

## Project

- Project: [Media Asset Ingest Roadmap](https://github.com/users/ankit-singhal87/projects/2)
- Repository: [ankit-singhal87/media-asset-ingest](https://github.com/ankit-singhal87/media-asset-ingest)

## Tracker Model

- GitHub milestones map to `MILESTONE-*` delivery slices.
- Epic issues map to milestone-level delivery slices.
- Story issues map to `USER-STORY-*` and are sub-issues of the owning epic.
- Task issues should be created as sub-issues of the relevant story.
- Bug issues should use `type:bug`, link to affected stories, and use GitHub
  dependencies when a bug blocks planned work.
- GitHub issue dependencies represent blocked-by relationships.
- Project fields carry `Type`, primary `Lane`, and `Status`; labels carry
  secondary lanes and searchable metadata.
- `docs/plans/active-worktrees.md` is only the local active-worktree mirror.

## Hybrid Agent Standard

Agents must keep repo automation docs and GitHub tracker state aligned without
duplicating relationship metadata in issue bodies.

Use this split:

- GitHub sub-issues: parent epic, story, and task hierarchy.
- GitHub dependencies: blocked-by relationships.
- GitHub milestones: delivery slice membership.
- GitHub Project fields: type, primary lane, status, branch/worktree, target
  files, and validation metadata.
- GitHub labels: searchable type/status/lane metadata and secondary lanes.
- Issue body: problem statement, outcomes or acceptance themes, target
  components, and implementation notes that are not already native metadata.
- Repo docs: durable operating rules, standards, architecture, and automation
  guidance.

Do not add `Parent Epic`, `Dependencies`, `Related Epic`, `Related Milestone`,
or similar relationship sections to issue bodies unless GitHub lacks a native
relationship for that specific fact.

When a task changes workflow or tracker behavior:

1. Update the relevant repo docs first.
2. Update the GitHub Project, issues, milestones, labels, fields, and
   relationships as needed.
3. Run a read-only tracker command to verify the remote shape.
4. Run local validation.
5. Report both local validation and GitHub verification evidence.

## GitHub Tool Decision Matrix

Prefer the GitHub plugin from
[openai/plugins](https://github.com/openai/plugins) for normal structured
GitHub operations. It avoids shell parsing and returns typed issue, PR,
repository, diff, review, and CI data directly.

Use the GitHub plugin for:

- Reading or updating issues, labels, assignees, and comments.
- Reading or creating pull requests.
- Fetching PR metadata, changed files, diffs, review threads, and reviews.
- Reading commits, comparing refs, and checking commit status.
- Reading workflow runs, jobs, logs, and artifacts.
- Merging or updating PRs when GitHub Projects fields are not involved.

Use `gh`, `gh api`, or the Make helpers for tracker operations the plugin does
not currently cover cleanly:

- GitHub Projects v2 field and item updates.
- Native sub-issue and dependency relationship APIs.
- Existing read-only tracker validation commands:
  - `make github-project-check`
  - `make github-project-summary`
  - `make github-project-hierarchy`
  - `make github-project-active`
- Any workflow already encoded in `scripts/dev/github-projects.sh`.

For common tracker writes, prefer the guarded helper commands in
`scripts/dev/github-projects.sh` over ad hoc `gh` invocations:

```bash
sh scripts/dev/github-projects.sh set-status 30 "In Progress"
sh scripts/dev/github-projects.sh set-type 30 Task
sh scripts/dev/github-projects.sh set-lane 30 Forge
sh scripts/dev/github-projects.sh set-text 30 "Worktree / Branch" "TASK-2-1 / .worktrees/TASK-2-1"
sh scripts/dev/github-projects.sh add-sub-issue 26 30
sh scripts/dev/github-projects.sh add-blocked-by 31 30
sh scripts/dev/github-projects.sh audit-fields
sh scripts/dev/github-projects.sh lint-issue-bodies
sh scripts/dev/github-projects.sh open-pr 30 TASK-2-1-create-dotnet-solution \
  "TASK-2-1: Create .NET solution skeleton" /tmp/pr-body.md docs/plans/active-worktrees.md
```

Run `make github-projects-script-test` to test those wrappers locally without
network access. `open-pr` creates the PR, marks the Project item `PR Open`,
sets the Project `PR` text field, and updates the matching branch row in
`docs/plans/active-worktrees.md`.

Default rule:

- Plugin first for issue, PR, review, diff, commit, and CI operations.
- `gh` or Make for GitHub Projects, native relationship wiring, and repo-local
  tracker validation.

Plugin smoke test evidence from 2026-05-03:

- `_get_user_login` returned `ankit-singhal87`.
- `_get_repo` returned `ankit-singhal87/media-asset-ingest`.
- `_fetch_issue` returned #30 `TASK-2-1: Create .NET solution skeleton`.
- `_get_pr_info` returned merged PR #29.

## Current Hierarchy

- #2 `MILESTONE-1: Documentation And Local Foundation`
- #3 `MILESTONE-2: .NET Solution And Local Runtime`
  - #26 `USER-STORY-16: Develop With Docker-First Tooling`
- #4 `MILESTONE-3: Ingest Package Lifecycle`
  - #11 `USER-STORY-1: Watch Ingest Mount`
  - #12 `USER-STORY-2: Start Only When Manifest Exists`
  - #13 `USER-STORY-3: Ingest All Discovered Files`
  - #14 `USER-STORY-4: Reconcile On Done Marker`
  - #15 `USER-STORY-5: Classify Media Essences`
- #5 `MILESTONE-4: Messaging And Outbox`
  - #16 `USER-STORY-6: Route Work Through ASB Queues`
  - #18 `USER-STORY-8: Use Transactional Outbox`
- #6 `MILESTONE-5: Dapr Workflow Orchestration`
  - #19 `USER-STORY-9: Orchestrate Package Lifecycle With Dapr`
  - #20 `USER-STORY-10: Support Nested Workflows`
- #7 `MILESTONE-6: Specialized Agents`
  - #17 `USER-STORY-7: Process With Specialized Agents`
- #8 `MILESTONE-7: Observability`
  - #21 `USER-STORY-11: Record Agent Progress`
- #9 `MILESTONE-8: Workflow Visualization UI`
  - #22 `USER-STORY-12: Visualize Workflow Execution`
  - #23 `USER-STORY-13: Inspect Node Logs`
  - #24 `USER-STORY-14: Drill Into Nested Workflows`
  - #25 `USER-STORY-15: Navigate Back From Child Workflows`
- #10 `MILESTONE-9: Kubernetes And Azure Readiness`
  - #27 `USER-STORY-17: Deploy To Kubernetes`

## CLI Notes

The GitHub CLI stores the token in `~/.config/gh/hosts.yml` because the local
credential store is not available in this environment. Do not commit or print
that file.

```bash
gh auth status
make github-project-check
make github-project-summary
make github-project-hierarchy
make github-project-active
```

Useful commands:

```bash
gh project view 2 --owner ankit-singhal87
gh project item-list 2 --owner ankit-singhal87 --limit 100
gh issue create --project "Media Asset Ingest Roadmap"
```

Creating or updating GitHub remote tracker state requires network access and a
GitHub token with `repo` and `project` scopes.

The Make targets are read-only helpers except
`make github-projects-script-test`, which uses a fake `gh` binary and never
contacts GitHub. Tracker write operations still require explicit authorization
before running the underlying helper command.
