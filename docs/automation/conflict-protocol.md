# Conflict Protocol

Use this when planned work conflicts with existing docs, task boundaries, or
parallel execution.

## File Scope Conflict

If a task needs a file outside its target file list:

1. Stop before editing.
2. Report the missing file path.
3. Explain why the file is needed.
4. Ask to update the task scope or split the task.

## Architecture Conflict

If implementation requires changing an ADR or architecture boundary:

1. Stop before implementation.
2. Identify the conflicting ADR or architecture doc.
3. Propose the decision that needs to change.
4. Update ADR/design docs only after approval.

## Parallel Agent Conflict

If another active task owns the same files:

1. Stop the later task.
2. Report overlapping files and task IDs.
3. Serialize work or re-slice target files.

## Validation Conflict

If validation fails for an unclear reason:

1. Stop broad implementation.
2. Capture command and relevant failure lines.
3. Investigate only within assigned scope.
4. Escalate if the cause crosses ownership lanes.
