# Terminal Scripts

`.terminals/` is a local-only directory for generated helper scripts that
launch parallel worktree tasks in separate terminal sessions. It is gitignored
and must stay out of commits.

Scripts in `.terminals/` are disposable coordination helpers. They may encode a
task scope, worktree branch name, target files, forbidden files, validation
command, and stop conditions so a local operator can start independent tracks
consistently.

Track launch scripts should create or enter the assigned worktree, then pass the
track prompt directly to `codex --cd <worktree>`. Printing the prompt without
starting Codex is useful only for manual debugging, not normal parallel work.

Keep generated helpers POSIX `sh` compatible when practical. Operators may run
them as `sh .terminals/<track>.sh`, which bypasses the script shebang; avoid
Bash-only shell options such as `set -o pipefail` unless the launch command
explicitly invokes Bash.

Durable task state does not live in `.terminals/`. Keep durable coordination in:

- `docs/plans`
- GitHub issues and pull requests
- `docs/plans/active-worktrees.md` while local worktrees are active

Regenerate or delete `.terminals/` scripts when the parallel plan changes. Do
not treat them as project source, architecture, or long-term automation.
