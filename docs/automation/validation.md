# Validation Matrix

| Change type | Minimal validation | Stronger validation | Docker? | Cloud? | Cost |
| --- | --- | --- | --- | --- | --- |
| Docs-only change | `make docs-check` | `make docs-fix` before committing when formatting/link checks fail | no | no | cheap |
| Architecture/ADR change | targeted term search for contradictory decisions | review affected ADRs and architecture docs together | no | no | cheap |
| Automation-doc change | `make validate-automation` and `make docs-check` | `make validate`, then read `AGENTS.md`, task workflow, and automation docs for consistency | no | no | cheap |
| Automation process docs only | `make docs-check` and `git diff --check` | `make validate` before PR when command docs, scripts, or Makefile targets also changed | no | no | cheap |
| Standards change | `make docs-check` and the relevant focused validation target | `make validate` and review affected automation docs and task workflow | no | no | cheap |
| Tooling change | `make validate-summary` and `make check-tools` when host tools are expected | `make validate`, then `make print-install-tools` review | no | no | cheap |
| .NET code change | focused `make test-dotnet-*` target for the touched component | `make test-dotnet`, then `make validate` before PR | yes when host `dotnet` is unavailable | no | moderate |
| Generic command runner change | `make test-dotnet-command-runner` | `make test-dotnet`, then `make validate` before PR | yes when host `dotnet` is unavailable | no | moderate |
| React control-plane change | `make test-ui` | `make test-ui` plus `make validate` before PR when backend contracts also changed | no | no | cheap |
| Local ingest smoke script change | `sh -n scripts/dev/local-e2e-smoke.sh` and `sh scripts/dev/local-e2e-smoke.sh --dry-run` | Run `dotnet run --project src/MediaIngest.Api --urls http://127.0.0.1:5000`, then `sh scripts/dev/local-e2e-smoke.sh` from another terminal to verify output files plus workflow command nodes | no | no | cheap |
| Local Compose validation script change | `sh -n scripts/dev/local-compose-check.sh`, `sh scripts/dev/local-compose-check.sh --dry-run`, `sh scripts/dev/local-compose-check.sh --runtime-smoke --dry-run`, and `sh scripts/dev/local-compose-check.sh` | `make local-compose-check`, `make local-runtime-smoke` when Docker is available, then `make validate` before PR creation | yes for default and runtime validation only | no | cheap to moderate |
| Docker/Kubernetes static asset change | `make docs-check`, `kubectl kustomize deploy/k8s`, `kubectl kustomize deploy/dapr/k8s`, and `git diff --check` | `kubectl apply --dry-run=client -k deploy/k8s` only when kubectl has an approved reachable local context | optional | no | cheap |
| Docker/Kubernetes runtime behavior change | relevant local smoke test once scripts exist | full local runtime validation | yes | no | moderate |
| Azure deployment change | static manifest/terraform validation only | manual cloud validation after approval | maybe | yes | paid/approval |

Do not run expensive Docker or cloud validation for docs-only edits.

Agents must report validation commands and outcomes before claiming completion.

## Validation Selection

Use changed paths to pick the cheapest sufficient validation:

- `Makefile`, `package.json`, or `scripts/dev/*`: run `make validate-summary`.
- `web/*`: run `make test-ui`.
- `docs/automation/*`: run `make docs-check` and `git diff --check`.
- `src/MediaIngest.Worker.CommandRunner/*` or
  `tests/MediaIngest.Worker.CommandRunner.Tests/*`: run
  `make test-dotnet-command-runner`.
- Other `src/*` or `tests/*`: run the focused `make test-dotnet-*` target for
  the component, then `make validate` before PR.
- Docker or Kubernetes files: use the Docker/Kubernetes rows above and avoid
  cloud validation unless explicitly approved.

For broad commands such as `make validate`, `make test-dotnet`, and
`make test-ui`, prefer the matching `*-summary` target during agent sessions.
The summary wrapper writes the full log to `/tmp` and prints the command, exit
code, key success or failure lines, and log path.

Run `make pr-readiness-check` before reporting PR readiness to print the local
changed-path summary, state-record status, and validation reminders.
