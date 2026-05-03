# TASK-<milestone>-<number>: <Title>

## Status

Planned

## Linked Work

- GitHub issue: #<number>
- Milestone: MILESTONE-<number>
- User stories:
  - USER-STORY-<number>
- Bugs:
  - None

Native GitHub relationships:

- Parent issue: #<number>
- Blocked by: #<number> or none
- Blocks: #<number> or none

## Ownership

Primary lane: <lane>

Supporting lanes:

- <lane>

## Target Files

Agents may edit only these files unless they escalate:

- `<path>`

## Investigation Targets

Read before editing:

- GitHub issue #<number> and its parent/sub-issue/dependency relationships.
- `<path>` - <why it matters>

## BDD Scenario

```gherkin
Scenario: <behavior>
  Given <context>
  When <action>
  Then <outcome>
```

## TDD Expectations

RED:

```bash
<command expected to fail before implementation>
```

GREEN:

```bash
<command expected to pass after implementation>
```

REFACTOR:

- Keep the same command passing after cleanup.

## Implementation Notes

- <specific notes or constraints>

## Validation

Minimal validation:

```bash
<command>
```

GitHub tracker validation:

```bash
make github-project-summary
make github-project-hierarchy
```

## Required Documentation Updates

- `docs/plans/task-index.md`
- `docs/status/work-log.md`
- <other docs>

## Completion Checklist

- [ ] Scope and ownership lane confirmed.
- [ ] GitHub issue and Project item checked.
- [ ] Parent/sub-issue/dependency relationships checked.
- [ ] Investigation targets reviewed.
- [ ] BDD scenario confirmed or updated.
- [ ] Failing test observed before production code, unless exception documented.
- [ ] Implementation completed within target files.
- [ ] Validation command run and evidence recorded.
- [ ] Required docs updated.
- [ ] GitHub Projects updated when tracker state changed.
- [ ] Handoff result prepared.

## Handoff Result

```text
status: completed | blocked | escalation-needed
taskId: TASK-<milestone>-<number>
github:
  issues:
    - #<number>
  projectValidation:
    - <command and evidence>
stories:
  - USER-STORY-<number>
filesChanged:
  - <path>
validation:
  - command: <command>
    outcome: <pass/fail/not-run>
docsUpdated:
  - <path>
blockers:
  - <none or details>
next:
  - <recommended next action>
```
