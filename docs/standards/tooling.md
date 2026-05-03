# Tooling Standards

## Supported Development Platform

Linux is the supported developer workstation platform for this repository.
Ubuntu/Debian are the first-class installation target for bootstrap scripts.

## Canonical Entrypoints

Use:

- `make agent-preflight` to print local agent startup context and validation
  options.
- `make check-tools` to verify host tools.
- `make install-tools` to install or print installation guidance for host tools.
- `make install-optional-tools` to install optional runtime and cloud CLIs
  after explicit local confirmation.
- `make print-install-optional-tools` to inspect optional install commands
  without running them.
- `make test-dotnet` to build and smoke-test the .NET solution.
- `make test-dotnet-*` for focused .NET smoke tests during inner-loop work.
- `make validate` for cheap repository validation.
- `make validate-docs` and `make validate-automation` for focused validation.
- `make docs-fix` to apply safe docs formatting fixes before committing.
- `make github-projects-script-test` to test legacy GitHub tracker helper
  wrappers without network access.
- `make github-project-check` to verify GitHub CLI auth and Project access.
- `make github-project-summary` to summarize tracker counts.
- `make github-project-hierarchy` to inspect parent/child issue hierarchy when
  needed.
- `make github-project-active` to list active tracker items.
- `npm run docs:check` for docs placeholder checks.
- `npm run docs:fix` for safe docs formatting fixes.
- `npm run dotnet:test` for .NET solution validation through npm.
- `npm run dotnet:test:<target>` for focused .NET smoke tests.
- `npm run github-project:script-test` for legacy tracker helper wrapper tests.
- `npm run tools:check` for host tool checks through npm.
- `npm run tools:print-install:optional` for optional install command review.

## Tooling Strategy

Prefer Docker-first development. Keep host installs small and run application
SDKs or cloud CLIs in containers where this keeps setup repeatable.

Host-native installs are allowed when they improve day-to-day development, but
they should be optional unless the workflow cannot work reliably in containers.

## Required Host Tools

Initial required tools:

- `bash`
- `git`
- `make`
- `curl`
- `jq`
- `node`
- `npm`
- `docker`

## Optional Host Tools

These tools may be installed locally, but project Makefile targets should prefer
containerized equivalents where practical:

- `dotnet`
- `kubectl`
- `helm`
- `dapr`
- `az`

Notes:

- .NET SDK: prefer SDK containers for build/test targets; host install is
  optional. `make test-dotnet` uses host `dotnet` when present and otherwise
  runs the .NET SDK container image from `DOTNET_SDK_IMAGE`, defaulting to
  `mcr.microsoft.com/dotnet/sdk:10.0`. Docker-backed .NET validation stores
  NuGet packages and CLI home data under `.cache/dotnet` by default so repeated
  agent runs reuse restore artifacts without committing generated files. Override
  the cache location with `DOTNET_REPO_CACHE_DIR` when needed.
- Azure CLI: prefer the official Azure CLI container or manual host install; login
  remains manual either way.
- kubectl/Helm/Dapr: host installs are convenient, but deployment-oriented
  targets can use a project tools container later.

## Adding Tools

If a task introduces a new required development tool, agents must update:

- `README.md` quickstart when the developer onboarding path changes
- this file
- `scripts/dev/check-tools.sh`
- `scripts/dev/install-tools.sh` when automated installation is appropriate
- `docs/automation/commands.md` if the tool adds a canonical command
- `docs/automation/validation.md` if validation behavior changes
- `package.json` scripts if the tool is part of repo-level npm automation

## Installation Rules

- Installation scripts must not perform cloud login.
- Installation scripts must not create paid cloud resources.
- Installation scripts may use `sudo` only after the developer intentionally
  runs them locally.
- Prefer explicit package sources and version-aware commands.
- If a tool cannot be installed safely by script, print manual instructions.
- Prefer containerized tool execution for heavyweight SDKs and cloud CLIs.
