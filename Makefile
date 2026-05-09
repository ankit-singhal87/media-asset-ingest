.PHONY: help agent-preflight pr-readiness-check check-tools install-tools install-optional-tools print-install-tools print-install-optional-tools up down local-compose-check local-runtime-smoke validate validate-summary validate-docs validate-docs-summary validate-automation validate-automation-summary test-dotnet test-dotnet-summary test-dotnet-foundation test-dotnet-contracts test-dotnet-watcher test-dotnet-api test-dotnet-persistence test-dotnet-outbox test-dotnet-workflow test-dotnet-observability test-dotnet-command-runner test-ui test-ui-summary docs-check docs-check-summary docs-fix scripts-check github-projects-script-test github-project-check github-project-summary github-project-hierarchy github-project-active github-project-audit-fields github-issue-body-lint summary-validation-script-test

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
	@printf '%s\n' "  make github-projects-script-test Test GitHub tracker helper wrappers"
	@printf '%s\n' "  make github-project-check     Verify GitHub CLI auth and project access"
	@printf '%s\n' "  make github-project-summary   Print lightweight tracker counts"
	@printf '%s\n' "  make github-project-active    Print active story board items"
	@printf '%s\n' "  make github-project-hierarchy Legacy hierarchy inspection"
	@printf '%s\n' "  make github-project-audit-fields Legacy detailed field audit"
	@printf '%s\n' "  make github-issue-body-lint   Legacy relationship metadata check"

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

validate: docs-check scripts-check github-projects-script-test summary-validation-script-test test-dotnet

validate-summary:
	@sh scripts/dev/validation-summary.sh make validate

validate-docs: docs-check

validate-docs-summary:
	@sh scripts/dev/validation-summary.sh make validate-docs

validate-automation: scripts-check github-projects-script-test summary-validation-script-test

validate-automation-summary:
	@sh scripts/dev/validation-summary.sh make validate-automation

test-dotnet:
	@sh scripts/dev/test-dotnet.sh

test-dotnet-summary:
	@sh scripts/dev/validation-summary.sh make test-dotnet

test-dotnet-foundation:
	@sh scripts/dev/test-dotnet.sh foundation

test-dotnet-contracts:
	@sh scripts/dev/test-dotnet.sh contracts

test-dotnet-watcher:
	@sh scripts/dev/test-dotnet.sh watcher

test-dotnet-api:
	@sh scripts/dev/test-dotnet.sh api

test-dotnet-persistence:
	@sh scripts/dev/test-dotnet.sh persistence

test-dotnet-outbox:
	@sh scripts/dev/test-dotnet.sh outbox

test-dotnet-workflow:
	@sh scripts/dev/test-dotnet.sh workflow

test-dotnet-observability:
	@sh scripts/dev/test-dotnet.sh observability

test-dotnet-command-runner:
	@sh scripts/dev/test-dotnet.sh command-runner

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
	@sh -n scripts/dev/test-dotnet.sh
	@sh -n scripts/dev/github-projects.sh
	@sh -n scripts/dev/test-github-projects.sh
	@sh -n scripts/dev/local-e2e-smoke.sh
	@sh -n scripts/dev/local-compose-check.sh

github-projects-script-test:
	@sh scripts/dev/test-github-projects.sh

summary-validation-script-test:
	@sh scripts/dev/test-summary-validation.sh

github-project-check:
	@sh scripts/dev/github-projects.sh check-auth

github-project-summary:
	@sh scripts/dev/github-projects.sh summary

github-project-hierarchy:
	@sh scripts/dev/github-projects.sh hierarchy

github-project-active:
	@sh scripts/dev/github-projects.sh active

github-project-audit-fields:
	@sh scripts/dev/github-projects.sh audit-fields

github-issue-body-lint:
	@sh scripts/dev/github-projects.sh lint-issue-bodies
