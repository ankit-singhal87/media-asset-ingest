# Automation Guide

Low-token operating entrypoint for AI coding agents and local automation.

## Read Order

1. [context.md](context.md)
2. [constraints.md](constraints.md)
3. [repo-map.md](repo-map.md)
4. [commands.md](commands.md)
5. [validation.md](validation.md)
6. [guardrails.md](guardrails.md)
7. [roles.md](roles.md)
8. [execution-checklist.md](execution-checklist.md)
9. [parallelization.md](parallelization.md) when dispatching parallel agents
10. [agent-handoff.md](agent-handoff.md) before reporting task results
11. [github-projects.md](github-projects.md) when work changes issues, milestones, or project state
12. [git-conventions.md](git-conventions.md) before creating branches, commits, or PRs
13. [terminal-scripts.md](terminal-scripts.md) when using local `.terminals/`
    helpers for parallel worktree launches

## Durable References

- [../product/user-stories.md](../product/user-stories.md) - product stories and acceptance themes.
- [../product/milestones.md](../product/milestones.md) - planned delivery slices.
- [../product/task-workflow.md](../product/task-workflow.md) - task lifecycle and required doc updates.
- [../plans/README.md](../plans/README.md) - milestone and task plan structure.
- [../bugs/README.md](../bugs/README.md) - defect report structure.
- [../architecture/000-system-overview.md](../architecture/000-system-overview.md) - system design and runtime decisions.
- [../adr](../adr) - architecture decision records.
- [../standards/dotnet.md](../standards/dotnet.md) - implementation standards entrypoint.
- [../status/current-status.md](../status/current-status.md) - current project state and next actions.
- [definition-of-done.md](definition-of-done.md) - completion criteria by work type.
- [conflict-protocol.md](conflict-protocol.md) - escalation rules for scope, architecture, parallel, and validation conflicts.
- [pr-checklist.md](pr-checklist.md) - pull request creation and merge checklist.
- [github-projects.md](github-projects.md) - GitHub Projects tracker model and CLI commands.
- [git-conventions.md](git-conventions.md) - branch, commit, and PR naming conventions.
- [terminal-scripts.md](terminal-scripts.md) - local-only helper script guidance for
  launching parallel worktree tasks.

Long-form project knowledge lives in product, architecture, ADR, and standards
documents. Automation docs should stay compact and execution-oriented.
