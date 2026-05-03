# Git Conventions

Use Git names that keep local work, GitHub issues, pull requests, and commits
easy to correlate.

## Branch Names

Start branch names with the lowest-level work item identifier available:

1. `TASK-<milestone>-<number>-<short-slug>` for implementation task issues.
2. `BUG-<number>-<short-slug>` for bug fixes.
3. `USER-STORY-<number>-<short-slug>` only when no task or bug issue exists yet.

Examples:

```text
TASK-2-1-create-dotnet-solution
BUG-1-fix-manifest-watch-race
USER-STORY-16-link-and-commit-standards
```

## Commit Messages

Start commit subjects with the same lowest-level identifier used by the branch.
Use the GitHub issue number in the commit body so GitHub links the commit to the
issue.

```text
TASK-2-1: Add solution skeleton

Refs #31
```

Use `Refs #<issue>` for incremental commits and `Closes #<issue>` only when the
commit or pull request completes the issue.

## Pull Requests

PR titles should start with the same identifier:

```text
TASK-2-1: Add solution skeleton
```

PR bodies must include linked issue references:

```text
Refs #31
Closes #31
```

Use `Closes` only for the lowest-level task or bug issue the PR completes.
Reference parent stories or epics with `Refs` unless they are truly completed by
the PR.

## Before Committing

Run safe docs fixes and validation before committing documentation changes:

```bash
make docs-fix
make validate
git diff --check
```

## Identifier Choice

Prefer the most specific item:

- Task issue beats story issue.
- Bug issue beats story issue.
- Story issue is acceptable for planning or standards work before task issues
  exist.

This keeps GitHub's branch, commit, and pull request links close to the work
actually being completed while still preserving story traceability through
native GitHub parent/sub-issue relationships.
