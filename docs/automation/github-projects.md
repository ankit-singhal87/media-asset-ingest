# GitHub Tracking

GitHub is a lightweight visibility layer. Repo-local product, architecture,
standards, and task docs remain the durable execution context.

## Project

- Project: [Media Asset Ingest Roadmap](https://github.com/users/ankit-singhal87/projects/2)
- Repository: [ankit-singhal87/media-asset-ingest](https://github.com/ankit-singhal87/media-asset-ingest)

## Lightweight Tracker Model

- Use GitHub issues for human-readable work descriptions.
- Use GitHub pull requests for review, validation evidence, and merge history.
- Keep parent/child issue relationships only when they improve navigation.
- Keep the GitHub Project as a simple status board: `Todo`, `In Progress`,
  `Done`, and optionally `Blocked`.
- Let PRs link issues naturally with `Refs #<issue>` or `Closes #<issue>`.

Do not require routine maintenance of:

- blocked-by dependency links
- custom Project fields for epic, story, task, bug, lane, target files, branch,
  worktree, or validation
- Project field audits
- worktree/branch metadata in GitHub

## Agent Rules

When work changes tracker state:

1. Update repo docs first when product, architecture, standards, or automation
   meaning changes.
2. Update only the relevant issue state and simple Project status when needed.
3. Put validation evidence in the PR body and final handoff, not Project fields.
4. Run one lightweight read-only check when tracker state changed.

Avoid Project reads and writes during normal implementation. Use them only at
task boundaries when visibility actually changes.

## Tool Choice

Prefer the GitHub plugin for structured repository, issue, PR, review, diff,
commit, comment, label, CI, and merge operations.

Use `gh` or Make helpers only when the plugin cannot cover the specific action,
especially for Project status board reads or writes, CLI auth checks, or local
wrapper validation.

Useful read-only checks:

```bash
gh auth status
make github-project-summary
make github-project-active
```

`make github-project-hierarchy` remains available when parent/child navigation
needs inspection, but it is not required for every task.

## Current Milestone Story Map

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
  - #16 `USER-STORY-6: Route Work Through ASB Command Topics`
  - #18 `USER-STORY-8: Use Transactional Outbox`
- #6 `MILESTONE-5: Dapr Workflow Orchestration`
  - #19 `USER-STORY-9: Orchestrate Package Lifecycle With Dapr`
  - #20 `USER-STORY-10: Support Nested Workflows`
- #7 `MILESTONE-6: Command Runners`
  - #17 `USER-STORY-7: Execute Generic Media Commands`
- #8 `MILESTONE-7: Observability`
  - #21 `USER-STORY-11: Record Command Runner Progress`
- #9 `MILESTONE-8: Workflow Visualization UI`
  - #22 `USER-STORY-12: Visualize Workflow Execution`
  - #23 `USER-STORY-13: Inspect Node Logs`
  - #24 `USER-STORY-14: Drill Into Nested Workflows`
  - #25 `USER-STORY-15: Navigate Back From Child Workflows`
- #10 `MILESTONE-9: Kubernetes And Azure Readiness`
  - #27 `USER-STORY-17: Deploy To Kubernetes`

## CLI Notes

The GitHub CLI stores the token in the user's home-directory host config because
the local credential store is not available in this environment. Do not commit
or print that file.
