# Subagent Prompt Templates

Use these compact prompts to keep subagent context bounded. Replace bracketed
values before dispatching.

## Read-Only Review

```text
Review only. Do not edit files.

Worktree: [absolute worktree path]
Scope:
- Files to inspect: [paths]
- Files not to inspect unless necessary: [paths or none]

Check:
- [specific requirements]
- no unrelated changes
- validation evidence is sufficient

Return:
status: APPROVED | CHANGES_REQUESTED
findings:
residualRisks:
notes:
```

## Implementation Worker

```text
You are not alone in the codebase. Do not revert or modify work outside your
ownership.

Worktree: [absolute worktree path]
Ownership:
- Modify only: [paths]
- Do not edit: [paths]

Task:
- [specific steps]

Validation:
- [commands]

Return:
status: DONE | BLOCKED
filesChanged:
validation:
notes:
Local commits are allowed after validation when they are small, scoped, and
use the branch identifier. Do not push or open a PR.
```

## PR Readiness Reviewer

```text
Read-only PR readiness review. Do not edit files.

Worktree: [absolute worktree path]

Check:
- branch follows docs/automation/git-conventions.md
- staged and unstaged paths are intentional
- required validation from docs/automation/validation.md has passed
- .worktrees/state/<worktree-slug>.md is current
- no secrets or generated local files are staged
- push/PR authorization is explicit

Return:
status: READY | NOT_READY
findings:
requiredBeforePR:
notes:
```

## CI Or Failure Investigator

```text
Investigate only. Do not edit files unless explicitly asked in a follow-up.

Worktree: [absolute worktree path]
Failure:
- Command/check: [name]
- Key output: [short excerpt]

Find:
- likely root cause
- minimal files involved
- recommended validation command

Return:
status: ROOT_CAUSE_FOUND | NEEDS_MORE_CONTEXT
rootCause:
evidence:
recommendedFix:
validation:
```
