# Worktree State Record Template

Use one ignored local state record per active worktree:
`.worktrees/state/<worktree-slug>.md`.

State records are local coordination checkpoints. Do not commit them.

```markdown
# <worktree-slug>

- Mode: answer-review | tiny-doc-hygiene | tooling-docs-behavior | feature-bug-implementation | parallel-subagent-implementation | post-merge-cleanup
- Worktree: `.worktrees/<worktree-slug>`
- Branch: `<branch-name>`
- Linked issue or PR: `<URL or not-applicable>`
- Story/task: `<USER-STORY/TASK/BUG or not-applicable>`
- Ownership lane: `<lane>`
- Target files:
  - `<path>`
- Forbidden files:
  - `<path or none>`
- Subagents:
  - Used: yes | no
  - Open: `<agent ids or none>`
  - Closed: `<agent ids or none>`
- Status: In Progress | Ready For Review | Ready For PR | PR Open | Merged | Blocked
- Validation:
  - `<command>`: passed | failed | not-run
- Next resume point: `<single sentence>`
- Cleanup:
  - Worktree removed: yes | no | not-applicable
  - Local branch deleted: yes | no | not-applicable
  - Remote branch deleted: yes | no | not-applicable
- Notes: `<short notes>`
```
