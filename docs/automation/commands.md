# Commands

| Command | Purpose | Cost | Docker | Cloud |
| --- | --- | --- | --- | --- |
| `git status --short` | Check working tree state. | cheap | no | no |
| `git worktree list` | Show active worktrees. | cheap | no | no |
| `rg "<term>" docs` | Targeted documentation search. | cheap | no | no |
| `find docs -maxdepth 3 -type f -print` | List documentation files. | cheap | no | no |
| `make help` | List canonical project commands. | cheap | no | no |
| `make check-tools` | Verify required Linux development tools and report optional tools. | cheap | no | no |
| `make install-tools` | Install supported Linux host tools after local confirmation. | moderate | no | no |
| `make print-install-tools` | Print installation commands without running them. | cheap | no | no |
| `make validate` | Run cheap repository validation. | cheap | no | no |
| `make github-project-check` | Verify GitHub CLI auth and project access. | cheap | no | no |
| `make github-project-summary` | Print GitHub Project, issue, milestone, and item counts. | cheap | no | no |
| `make github-project-hierarchy` | Print epic/story sub-issue hierarchy. | cheap | no | no |
| `make github-project-active` | Print in-progress GitHub Project items. | cheap | no | no |
| `npm run docs:check` | Check docs for unfinished placeholders. | cheap | no | no |
| `npm run github-project:check` | Verify GitHub CLI auth and project access through npm. | cheap | no | no |
| `npm run github-project:summary` | Print GitHub tracker counts through npm. | cheap | no | no |
| `npm run github-project:hierarchy` | Print GitHub tracker hierarchy through npm. | cheap | no | no |
| `npm run github-project:active` | Print in-progress GitHub tracker items through npm. | cheap | no | no |
| `npm run tools:check` | Verify required Linux development tools through npm. | cheap | no | no |

Add Makefile targets once the runtime scaffold exists. Prefer the cheapest
relevant validation command first.

When a task adds a canonical command, update this file and
`docs/automation/validation.md` when validation behavior changes.
