.PHONY: help agent-preflight pr-readiness-check check-tools install-tools install-optional-tools print-install-tools print-install-optional-tools up down local-compose-check local-runtime-smoke validate validate-summary validate-docs validate-docs-summary validate-automation validate-automation-summary test-dotnet test-dotnet-summary test-dotnet-contracts test-dotnet-contracts-summary test-dotnet-watcher test-dotnet-watcher-summary test-dotnet-api test-dotnet-api-summary test-dotnet-essence test-dotnet-essence-summary test-dotnet-persistence test-dotnet-persistence-summary test-dotnet-outbox test-dotnet-outbox-summary test-dotnet-workflow test-dotnet-workflow-summary test-dotnet-observability test-dotnet-observability-summary test-dotnet-command-runner test-dotnet-command-runner-summary test-ui test-ui-summary docs-check docs-check-summary docs-fix scripts-check summary-validation-script-test local-compose-check-script-test

help:
	@printf '%s\n' "Available targets:"
	@printf '%s\n' "  make agent-preflight     Print local agent startup context"
	@printf '%s\n' "  make pr-readiness-check  Print dry PR readiness checklist"
	@printf '%s\n' "  make check-tools         Verify required Linux development tools"
	@printf '%s\n' "  make install-tools       Install supported Linux development tools"
	@printf '%s\n' "  make install-optional-tools Install optional local runtime/cloud CLIs"
	@printf '%s\n' "  make print-install-tools Print manual installation commands"
	@printf '%s\n' "  make print-install-optional-tools Print optional tool install commands"
	@printf '%s\n' "  make up                  Start local Docker Compose runtime"
	@printf '%s\n' "  make down                Stop local Docker Compose runtime"
	@printf '%s\n' "  make local-compose-check Validate local Docker Compose configuration"
	@printf '%s\n' "  make local-runtime-smoke Start Compose and run local ingest smoke"
	@printf '%s\n' "  make validate            Run cheap repository validation"
	@printf '%s\n' "  make validate-summary    Run cheap validation with compact output and /tmp log"
	@printf '%s\n' "  make validate-docs       Run docs validation only"
	@printf '%s\n' "  make validate-docs-summary Run docs validation with compact output"
	@printf '%s\n' "  make validate-automation Run automation script validation only"
	@printf '%s\n' "  make validate-automation-summary Run automation validation with compact output"
	@printf '%s\n' "  make test-dotnet         Build and smoke-test the .NET solution"
	@printf '%s\n' "  make test-dotnet-summary Build and smoke-test .NET with compact output"
	@printf '%s\n' "  make test-dotnet-*       Run a focused .NET smoke target"
	@printf '%s\n' "  make test-ui             Run React control-plane Vitest tests"
	@printf '%s\n' "  make test-ui-summary     Run UI tests with compact output"
	@printf '%s\n' "  make docs-check          Check docs for unfinished placeholders"
	@printf '%s\n' "  make docs-fix            Apply safe docs formatting fixes"
	@printf '%s\n' "  make scripts-check       Syntax-check shell scripts"

agent-preflight:
	@sh scripts/dev/agent-preflight.sh

pr-readiness-check:
	@sh scripts/dev/pr-readiness-check.sh

check-tools:
	@sh scripts/dev/check-tools.sh

install-tools:
	@sh scripts/dev/install-tools.sh

install-optional-tools:
	@sh scripts/dev/install-optional-tools.sh

print-install-tools:
	@sh scripts/dev/install-tools.sh --print-only

print-install-optional-tools:
	@sh scripts/dev/install-optional-tools.sh --print-only

up:
	docker compose -f deploy/docker/compose.yaml up --build

down:
	docker compose -f deploy/docker/compose.yaml down

local-compose-check:
	@sh scripts/dev/local-compose-check.sh

local-runtime-smoke:
	@sh scripts/dev/local-compose-check.sh --runtime-smoke

validate: docs-check scripts-check summary-validation-script-test test-dotnet

validate-summary:
	@sh scripts/dev/validation-summary.sh make validate

validate-docs: docs-check

validate-docs-summary:
	@sh scripts/dev/validation-summary.sh make validate-docs

validate-automation: scripts-check summary-validation-script-test local-compose-check-script-test

validate-automation-summary:
	@sh scripts/dev/validation-summary.sh make validate-automation

test-dotnet:
	@sh scripts/dev/test-dotnet.sh

test-dotnet-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet

test-dotnet-contracts:
	@sh scripts/dev/test-dotnet.sh contracts

test-dotnet-contracts-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-contracts

test-dotnet-watcher:
	@sh scripts/dev/test-dotnet.sh watcher

test-dotnet-watcher-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-watcher

test-dotnet-api:
	@sh scripts/dev/test-dotnet.sh api

test-dotnet-api-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-api

test-dotnet-essence:
	@sh scripts/dev/test-dotnet.sh essence

test-dotnet-essence-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-essence

test-dotnet-persistence:
	@sh scripts/dev/test-dotnet.sh persistence

test-dotnet-persistence-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-persistence

test-dotnet-outbox:
	@sh scripts/dev/test-dotnet.sh outbox

test-dotnet-outbox-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-outbox

test-dotnet-workflow:
	@sh scripts/dev/test-dotnet.sh workflow

test-dotnet-workflow-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-workflow

test-dotnet-observability:
	@sh scripts/dev/test-dotnet.sh observability

test-dotnet-observability-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-observability

test-dotnet-command-runner:
	@sh scripts/dev/test-dotnet.sh command-runner

test-dotnet-command-runner-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet-command-runner

test-ui:
	@npm run ui:test

test-ui-summary:
	@sh scripts/dev/validation-summary.sh make test-ui

docs-check:
	@npm run docs:check

docs-check-summary:
	@sh scripts/dev/validation-summary.sh make docs-check

docs-fix:
	@npm run docs:fix

scripts-check:
	@sh -n scripts/dev/check-tools.sh
	@sh -n scripts/dev/install-tools.sh
	@sh -n scripts/dev/install-optional-tools.sh
	@sh -n scripts/dev/agent-preflight.sh
	@sh -n scripts/dev/pr-readiness-check.sh
	@sh -n scripts/dev/validation-summary.sh
	@sh -n scripts/dev/test-summary-validation.sh
	@sh -n scripts/dev/test-local-compose-check.sh
	@sh -n scripts/dev/test-dotnet.sh
	@sh -n scripts/dev/local-e2e-smoke.sh
	@sh -n scripts/dev/local-compose-check.sh

summary-validation-script-test:
	@sh scripts/dev/test-summary-validation.sh

local-compose-check-script-test:
	@sh scripts/dev/test-local-compose-check.sh
