# MILESTONE-<number>: <Title> Plan

## Status

Planned

## Linked User Stories

- USER-STORY-<number>

## GitHub Tracker

- Milestone: MILESTONE-<number>
- Epic issue: #<number>
- Project: Media Asset Ingest Roadmap

## Goal

<one paragraph describing what this milestone delivers>

## Non-Goals

- <explicit non-goal>

## Ownership

Primary lane: <lane>

Supporting lanes:

- <lane>

## Domains

- <domain>

## Components

- `<path-or-planned-component>`

## Dependencies

- Native GitHub blocked-by relationships plus any ADR or external dependency
  that cannot be represented in GitHub relationships.

## Execution Strategy

Preferred approach: vertical slice

Rationale:

- <why this split creates usable, testable progress>

## Task Breakdown

| Task ID | GitHub issue | Title | Stories | Primary lane | Target files | Local status |
| --- | --- | --- | --- | --- | --- |
| TASK-<milestone>-1 | #<number> | <title> | USER-STORY-<number> | <lane> | `<path>` | Planned |

## Validation Strategy

Minimal validation:

```bash
<command>
```

Stronger validation:

```bash
<command>
```

GitHub tracker validation:

```bash
make github-project-summary
make github-project-hierarchy
```

## Documentation Updates Required

- `docs/status/current-status.md`
- `docs/status/work-log.md`
- `docs/product/milestones.md`
- `docs/product/user-stories.md`
- `docs/product/backlog.md`
- `docs/plans/task-index.md`
- GitHub Project fields, sub-issues, dependencies, and milestone status
- <other docs>

## Stop Conditions

- Secrets, credentials, cloud billing, or external account access is required.
- A task needs files outside its declared target file list.
- A schema, contract, or architecture decision conflicts with existing docs.
- Validation fails for an unclear reason.

## Definition Of Done

- All planned tasks are complete or explicitly deferred.
- Linked user stories reflect current status.
- GitHub Project and issue relationships reflect current status.
- Validation evidence is recorded.
- Required documentation updates are complete.
- Known bugs are filed in `docs/bugs`.
