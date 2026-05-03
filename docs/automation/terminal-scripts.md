# Terminal Scripts

`.terminals/` is a local-only directory for generated helper scripts that
launch parallel worktree tasks in separate terminal sessions. It is gitignored
and must stay out of commits.

Scripts in `.terminals/` are disposable coordination helpers. They may encode a
task scope, worktree branch name, target files, forbidden files, validation
command, and stop conditions so a local operator can start independent tracks
consistently.

Durable task state does not live in `.terminals/`. Keep durable coordination in:

- `docs/plans`
- GitHub issues and pull requests
- `docs/plans/active-worktrees.md` while local worktrees are active

Regenerate or delete `.terminals/` scripts when the parallel plan changes. Do
not treat them as project source, architecture, or long-term automation.
