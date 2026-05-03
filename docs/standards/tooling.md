# Tooling Standards

## Supported Development Platform

Linux is the supported developer workstation platform for this repository.
Ubuntu/Debian are the first-class installation target for bootstrap scripts.

## Canonical Entrypoints

Use:

- `make check-tools` to verify host tools.
- `make install-tools` to install or print installation guidance for host tools.
- `make validate` for cheap repository validation.
- `npm run docs:check` for docs placeholder checks.
- `npm run tools:check` for host tool checks through npm.

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

- .NET SDK: prefer SDK containers for build/test targets; host install is optional.
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
