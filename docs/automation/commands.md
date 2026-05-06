# Commands

## Output Discipline

Prefer narrow commands and summarized evidence. Use `git diff --name-only`,
`git diff --stat`, targeted `sed`, and focused `rg` before full diffs or broad
logs.

When reporting results, include command names, pass/fail outcome, and the small
piece of output that proves the claim. Avoid pasting full logs unless a failure
requires it.

## Shared Mount Notes

This local repository is often used from a shared mount. Use these workarounds
only when the mount behavior requires them:

- Add `-c core.filemode=false` to Git inspection commands when executable-bit
  noise hides the real content diff.
- Add a command-local `-c safe.directory=<absolute-worktree-path>` when Git
  rejects a worktree with dubious ownership. Prefer command-local overrides over
  changing global Git config.
- Use `npm ci --no-bin-links --prefix web/ingest-control-plane` when installing
  UI dependencies on mounts that do not support npm `.bin` symlink creation.

| Command | Purpose | Cost | Docker | Cloud |
| --- | --- | --- | --- | --- |
| `git status --short` | Check working tree state. | cheap | no | no |
| `git worktree list` | Show active worktrees. | cheap | no | no |
| `rg "<term>" docs` | Targeted documentation search. | cheap | no | no |
| `find docs -maxdepth 3 -type f -print` | List documentation files. | cheap | no | no |
| `make help` | List canonical project commands. | cheap | no | no |
| `make agent-preflight` | Print local startup context, worktrees, tool status, and validation targets. | cheap | no | no |
| `make pr-readiness-check` | Print a local dry PR readiness checklist, staged/unstaged paths, state records, and suggested validation. | cheap | no | no |
| `make check-tools` | Verify required Linux development tools and report optional tools. | cheap | no | no |
| `make install-tools` | Install supported Linux host tools after local confirmation. | moderate | no | no |
| `make install-optional-tools` | Install optional host runtime and cloud CLIs after local confirmation. | moderate | no | no |
| `make print-install-tools` | Print installation commands without running them. | cheap | no | no |
| `make print-install-optional-tools` | Print optional host tool installation commands without running them. | cheap | no | no |
| `make up` | Start the local API, UI, and PostgreSQL Docker Compose runtime with image builds. | moderate | yes | no |
| `make down` | Stop the local Docker Compose runtime. | cheap | yes | no |
| `make local-compose-check` | Validate `deploy/docker/compose.yaml` with `docker compose config` without starting containers. | cheap | yes | no |
| `make local-runtime-smoke` | Start the local Compose API, UI, and PostgreSQL runtime, wait for API/UI HTTP responses, run the local ingest smoke script, and stop the stack. | moderate | yes | no |
| `make validate` | Run cheap repository validation. | cheap | no | no |
| `make validate-docs` | Run documentation validation only. | cheap | no | no |
| `make validate-automation` | Run shell syntax checks and GitHub helper wrapper tests. | cheap | no | no |
| `dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000` | Start the local ingest API on the fixed development port. | cheap | no | no |
| `cd web/ingest-control-plane && npm run dev` | Start the React control plane with Vite `/api` proxying to the local API. | cheap | no | no |
| `sh scripts/dev/local-e2e-smoke.sh --dry-run` | Print the local manifest ingest E2E smoke plan without changing files or calling the API. Use `SMOKE_EXPECT_COPIED_FILES=manifest` to plan manifest-only copied output assertions with command-node evidence. | cheap | no | no |
| `sh scripts/dev/local-e2e-smoke.sh` | Post local ingest start, create a smoke package, assert copied output files, and verify the workflow graph exposes routed command nodes. Requires the API to be running locally. Use `SMOKE_EXPECT_COPIED_FILES=manifest` when the runtime should assert only the copied manifest pair plus command-node evidence. | cheap | no | no |
| `sh scripts/dev/local-compose-check.sh --dry-run` | Print the API/UI/PostgreSQL Compose validation plan without changing files or starting containers. | cheap | no | no |
| `sh scripts/dev/local-compose-check.sh` | Run `docker compose -f deploy/docker/compose.yaml config` and report resolved local services. | cheap | yes | no |
| `sh scripts/dev/local-compose-check.sh --runtime-smoke` | Start the local Compose runtime with project name `media-asset-ingest-local`, wait for API/UI HTTP responses, run `scripts/dev/local-e2e-smoke.sh` with manifest-output assertions and command-node evidence, and stop the stack. Override `LOCAL_COMPOSE_API_PORT`, `LOCAL_COMPOSE_UI_PORT`, or `LOCAL_COMPOSE_POSTGRES_PORT` when default local ports are busy; the script exports the current UID/GID for host-writable smoke output. | moderate | yes | no |
| `sh scripts/dev/local-compose-check.sh --runtime-smoke --dry-run` | Print the local Compose runtime smoke plan without requiring Docker, changing files, starting containers, or calling local HTTP endpoints. | cheap | no | no |
| `make test-dotnet` | Build and smoke-test the .NET solution using host `dotnet` or the .NET SDK container. | moderate | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-foundation` | Run the foundation smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-contracts` | Run the contracts smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-watcher` | Run the ingest watcher smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-persistence` | Run the persistence smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-outbox` | Run the outbox worker smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-workflow` | Run the workflow smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-observability` | Run the observability smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-command-runner` | Run the generic command runner smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-ui` | Run the React control-plane Vitest tests. | cheap | no | no |
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
| `npm run ui:test` | Run the React control-plane Vitest tests from the repo root. | cheap | no | no |
| `npm run pr:readiness` | Print the local dry PR readiness checklist through npm. | cheap | no | no |
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
