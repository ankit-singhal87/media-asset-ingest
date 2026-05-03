# Commands

| Command | Purpose | Cost | Docker | Cloud |
| --- | --- | --- | --- | --- |
| `git status --short` | Check working tree state. | cheap | no | no |
| `git worktree list` | Show active worktrees. | cheap | no | no |
| `rg "<term>" docs` | Targeted documentation search. | cheap | no | no |
| `find docs -maxdepth 3 -type f -print` | List documentation files. | cheap | no | no |
| `make help` | List canonical project commands. | cheap | no | no |
| `make agent-preflight` | Print local startup context, worktrees, tool status, and validation targets. | cheap | no | no |
| `make check-tools` | Verify required Linux development tools and report optional tools. | cheap | no | no |
| `make install-tools` | Install supported Linux host tools after local confirmation. | moderate | no | no |
| `make install-optional-tools` | Install optional host runtime and cloud CLIs after local confirmation. | moderate | no | no |
| `make print-install-tools` | Print installation commands without running them. | cheap | no | no |
| `make print-install-optional-tools` | Print optional host tool installation commands without running them. | cheap | no | no |
| `make validate` | Run cheap repository validation. | cheap | no | no |
| `make validate-docs` | Run documentation validation only. | cheap | no | no |
| `make validate-automation` | Run shell syntax checks and GitHub helper wrapper tests. | cheap | no | no |
| `make test-dotnet` | Build and smoke-test the .NET solution using host `dotnet` or the .NET SDK container. | moderate | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-foundation` | Run the foundation smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-contracts` | Run the contracts smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-watcher` | Run the ingest watcher smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-persistence` | Run the persistence smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-outbox` | Run the outbox worker smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-workflow` | Run the workflow smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-observability` | Run the observability smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make docs-fix` | Apply safe documentation formatting fixes before committing. | cheap | no | no |
| `make github-projects-script-test` | Test GitHub tracker helper wrappers without network. | cheap | no | no |
| `make github-project-check` | Verify GitHub CLI auth and project access. | cheap | no | no |
| `make github-project-summary` | Print GitHub Project, issue, milestone, and item counts. | cheap | no | no |
| `make github-project-hierarchy` | Print parent/child issue hierarchy when navigation needs inspection. | cheap | no | no |
| `make github-project-active` | Print in-progress GitHub Project items. | cheap | no | no |
| `make github-project-audit-fields` | Legacy helper for detailed Project fields; not required for lightweight tracking. | cheap | no | no |
| `make github-issue-body-lint` | Check issue bodies avoid duplicated relationship metadata. | cheap | no | no |
| `npm run docs:check` | Check docs for unfinished placeholders. | cheap | no | no |
| `npm run docs:fix` | Apply safe docs formatting fixes. | cheap | no | no |
| `npm run dotnet:test` | Build and smoke-test the .NET solution through npm. | moderate | yes when host `dotnet` is unavailable | no |
| `npm run dotnet:test:<target>` | Run a focused .NET smoke test target. | cheap | yes when host `dotnet` is unavailable | no |
| `npm run github-project:check` | Verify GitHub CLI auth and project access through npm. | cheap | no | no |
| `npm run github-project:summary` | Print GitHub tracker counts through npm. | cheap | no | no |
| `npm run github-project:hierarchy` | Print GitHub tracker hierarchy through npm. | cheap | no | no |
| `npm run github-project:active` | Print in-progress GitHub tracker items through npm. | cheap | no | no |
| `npm run github-project:audit-fields` | Legacy detailed Project field audit through npm; not required for lightweight tracking. | cheap | no | no |
| `npm run github-project:issue-body-lint` | Check issue body relationship metadata through npm. | cheap | no | no |
| `npm run github-project:script-test` | Test GitHub tracker helper wrappers through npm. | cheap | no | no |
| `npm run tools:check` | Verify required Linux development tools through npm. | cheap | no | no |
| `npm run tools:print-install:optional` | Print optional host tool installation commands through npm. | cheap | no | no |

Add Makefile targets once the runtime scaffold exists. Prefer the cheapest
relevant validation command first.

When a task adds a canonical command, update this file and
`docs/automation/validation.md` when validation behavior changes.
