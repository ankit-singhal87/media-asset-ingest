# TASK-<milestone>-<number>: <Title>

## Status

Planned

## Linked Work

- Milestone: MILESTONE-<number>
- User stories:
  - USER-STORY-<number>
- Bugs:
  - None

## Ownership

Primary lane: <lane>

Supporting lanes:

- <lane>

## Target Files

Agents may edit only these files unless they escalate:

- `<path>`

## Investigation Targets

Read before editing:

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

## Required Documentation Updates

- `docs/plans/task-index.md`
- `docs/status/work-log.md`
- <other docs>

## Completion Checklist

- [ ] Scope and ownership lane confirmed.
- [ ] Investigation targets reviewed.
- [ ] BDD scenario confirmed or updated.
- [ ] Failing test observed before production code, unless exception documented.
- [ ] Implementation completed within target files.
- [ ] Validation command run and evidence recorded.
- [ ] Required docs updated.
- [ ] Handoff result prepared.

## Handoff Result

```text
status: completed | blocked | escalation-needed
taskId: TASK-<milestone>-<number>
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
