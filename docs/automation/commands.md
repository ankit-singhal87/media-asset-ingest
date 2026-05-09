# Commands

## Output Discipline

Prefer narrow commands and summarized evidence. Use `git diff --name-only`,
`git diff --stat`, targeted `sed`, and focused `rg` before full diffs or broad
logs.

When reporting results, include command names, pass/fail outcome, and the small
piece of output that proves the claim. Avoid pasting full logs unless a failure
requires it.

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
| `make up` | Start the local API, UI, PostgreSQL, outbox worker, and command-runner Docker Compose runtime with image builds. | moderate | yes | no |
| `make down` | Stop the local Docker Compose runtime. | cheap | yes | no |
| `make local-compose-check` | Validate `deploy/docker/compose.yaml` with `docker compose config` without starting containers. | cheap | yes | no |
| `make local-runtime-smoke` | Start the local Compose API, UI, PostgreSQL, outbox worker, and command-runner runtime, wait for API/UI HTTP responses, run the local ingest smoke script, verify runner services/logs, query PostgreSQL durable state/outbox execution-class evidence, and stop the stack. | moderate | yes | no |
| `make validate` | Run cheap repository validation. | cheap | no | no |
| `make validate-summary` | Run `make validate`, capture the full log under `/tmp`, and print only the command, exit code, key success or failure lines, and log path. Prefer this before broad validation in agent sessions. | cheap | no | no |
| `make validate-docs` | Run documentation validation only. | cheap | no | no |
| `make validate-docs-summary` | Run docs validation with compact summary output and a full `/tmp` log. | cheap | no | no |
| `make validate-automation` | Run shell syntax checks plus local automation wrapper and dry-run plan tests. | cheap | no | no |
| `make validate-automation-summary` | Run automation validation with compact summary output and a full `/tmp` log. | cheap | no | no |
| `make summary-validation-script-test` | Test the summary validation wrapper without network or external services. | cheap | no | no |
| `dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000` | Start the local ingest API on the fixed development port. | cheap | no | no |
| `cd web/ingest-control-plane && npm run dev` | Start the React control plane with Vite `/api` proxying to the local API. | cheap | no | no |
| `sh scripts/dev/local-e2e-smoke.sh --dry-run` | Print the local manifest ingest E2E smoke plan without changing files or calling the API. Use `SMOKE_EXPECT_COPIED_FILES=manifest` to plan manifest-only copied output assertions with command-node evidence. | cheap | no | no |
| `sh scripts/dev/local-e2e-smoke.sh` | Post local ingest start, create a smoke package, assert copied output files, and verify the workflow graph exposes routed command nodes. Requires the API to be running locally. Use `SMOKE_EXPECT_COPIED_FILES=manifest` when the runtime should assert only the copied manifest pair plus command-node evidence. | cheap | no | no |
| `sh scripts/dev/local-compose-check.sh --dry-run` | Print the API/UI/PostgreSQL/outbox/command-runner Compose validation plan without changing files or starting containers. | cheap | no | no |
| `sh scripts/dev/local-compose-check.sh` | Run `docker compose -f deploy/docker/compose.yaml config` and report resolved local services. | cheap | yes | no |
| `sh scripts/dev/local-compose-check.sh --runtime-smoke` | Start the local Compose runtime with project name `media-asset-ingest-local`, wait for API/UI HTTP responses, verify light/medium/heavy command-runner services and startup logs, run `scripts/dev/local-e2e-smoke.sh` with manifest-output assertions and command-node evidence, query PostgreSQL for persisted package state plus dispatched command outbox rows grouped by `executionClass`, and stop the stack. Override `LOCAL_COMPOSE_API_PORT`, `LOCAL_COMPOSE_UI_PORT`, `LOCAL_COMPOSE_POSTGRES_PORT`, or `SMOKE_PACKAGE_ID` when needed; the script exports the current UID/GID for host-writable smoke output. | moderate | yes | no |
| `sh scripts/dev/local-compose-check.sh --runtime-smoke --dry-run` | Print the local Compose runtime smoke plan without requiring Docker, changing files, starting containers, or calling local HTTP endpoints. | cheap | no | no |
| `make test-dotnet` | Build and smoke-test the .NET solution using host `dotnet` or the .NET SDK container. | moderate | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-summary` | Run .NET solution validation with compact summary output and a full `/tmp` log. | moderate | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-contracts` | Run the contracts smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-watcher` | Run the ingest watcher smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-essence` | Run the essence checksum and classification smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-persistence` | Run the persistence smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-outbox` | Run the outbox worker smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-workflow` | Run the workflow smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-observability` | Run the observability smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-command-runner` | Run the generic command runner smoke test project only. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-dotnet-<target>-summary` | Run a focused .NET smoke target with compact summary output and a full `/tmp` log. | cheap | yes when host `dotnet` is unavailable | no |
| `make test-ui` | Run the React control-plane Vitest tests. | cheap | no | no |
| `make test-ui-summary` | Run UI tests with compact summary output and a full `/tmp` log. | cheap | no | no |
| `make docs-fix` | Apply safe documentation formatting fixes before committing. | cheap | no | no |
| `npm run docs:check` | Check docs for unfinished placeholders. | cheap | no | no |
| `npm run docs:check:summary` | Check docs with compact summary output and a full `/tmp` log. | cheap | no | no |
| `npm run docs:fix` | Apply safe docs formatting fixes. | cheap | no | no |
| `npm run dotnet:test` | Build and smoke-test the .NET solution through npm. | moderate | yes when host `dotnet` is unavailable | no |
| `npm run dotnet:test:summary` | Build and smoke-test the .NET solution through npm with compact summary output and a full `/tmp` log. | moderate | yes when host `dotnet` is unavailable | no |
| `npm run dotnet:test:<target>` | Run a focused .NET smoke test target. | cheap | yes when host `dotnet` is unavailable | no |
| `npm run ui:test` | Run the React control-plane Vitest tests from the repo root. | cheap | no | no |
| `npm run ui:test:summary` | Run UI tests with compact summary output and a full `/tmp` log. | cheap | no | no |
| `npm run validate:summary` | Run cheap repository validation with compact summary output and a full `/tmp` log. | cheap | no | no |
| `npm run pr:readiness` | Print the local dry PR readiness checklist through npm. | cheap | no | no |
| `npm run summary-validation:test` | Test the summary validation wrapper through npm. | cheap | no | no |
| `npm run tools:check` | Verify required Linux development tools through npm. | cheap | no | no |
| `npm run tools:print-install:optional` | Print optional host tool installation commands through npm. | cheap | no | no |

Add Makefile targets once the runtime scaffold exists. Prefer the cheapest
relevant validation command first. In agent sessions, prefer the `*-summary`
validation target for broad commands and open the full `/tmp` log only when the
summary does not explain a failure.

When a task adds a canonical command, update this file and
`docs/automation/validation.md` when validation behavior changes.

Project state, plans, milestones, bugs, and status are maintained in repo docs.
Remote GitHub operations are limited to repository, issue, and PR collaboration
when explicitly needed.
