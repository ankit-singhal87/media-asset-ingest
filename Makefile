.PHONY: help agent-preflight check-tools install-tools install-optional-tools print-install-tools print-install-optional-tools local-compose-check validate validate-docs validate-automation test-dotnet test-dotnet-foundation test-dotnet-contracts test-dotnet-watcher test-dotnet-api test-dotnet-persistence test-dotnet-outbox test-dotnet-workflow test-dotnet-observability docs-check docs-fix scripts-check github-projects-script-test github-project-check github-project-summary github-project-hierarchy github-project-active github-project-audit-fields github-issue-body-lint

help:
	@printf '%s\n' "Available targets:"
	@printf '%s\n' "  make agent-preflight     Print local agent startup context"
	@printf '%s\n' "  make check-tools         Verify required Linux development tools"
	@printf '%s\n' "  make install-tools       Install supported Linux development tools"
	@printf '%s\n' "  make install-optional-tools Install optional local runtime/cloud CLIs"
	@printf '%s\n' "  make print-install-tools Print manual installation commands"
	@printf '%s\n' "  make print-install-optional-tools Print optional tool install commands"
	@printf '%s\n' "  make local-compose-check Validate local Docker Compose configuration"
	@printf '%s\n' "  make validate            Run cheap repository validation"
	@printf '%s\n' "  make validate-docs       Run docs validation only"
	@printf '%s\n' "  make validate-automation Run automation script validation only"
	@printf '%s\n' "  make test-dotnet         Build and smoke-test the .NET solution"
	@printf '%s\n' "  make test-dotnet-*       Run a focused .NET smoke target"
	@printf '%s\n' "  make docs-check          Check docs for unfinished placeholders"
	@printf '%s\n' "  make docs-fix            Apply safe docs formatting fixes"
	@printf '%s\n' "  make scripts-check       Syntax-check shell scripts"
	@printf '%s\n' "  make github-projects-script-test Test GitHub tracker helper wrappers"
	@printf '%s\n' "  make github-project-check     Verify GitHub CLI auth and project access"
	@printf '%s\n' "  make github-project-summary   Print GitHub tracker counts"
	@printf '%s\n' "  make github-project-hierarchy Print epic/story hierarchy"
	@printf '%s\n' "  make github-project-active    Print in-progress project items"
	@printf '%s\n' "  make github-project-audit-fields Verify required Project fields"
	@printf '%s\n' "  make github-issue-body-lint   Check issue bodies for duplicated relationships"

agent-preflight:
	@sh scripts/dev/agent-preflight.sh

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

local-compose-check:
	@sh scripts/dev/local-compose-check.sh

validate: docs-check scripts-check github-projects-script-test test-dotnet

validate-docs: docs-check

validate-automation: scripts-check github-projects-script-test

test-dotnet:
	@sh scripts/dev/test-dotnet.sh

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

docs-check:
	@npm run docs:check

docs-fix:
	@npm run docs:fix

scripts-check:
	@sh -n scripts/dev/check-tools.sh
	@sh -n scripts/dev/install-tools.sh
	@sh -n scripts/dev/install-optional-tools.sh
	@sh -n scripts/dev/agent-preflight.sh
	@sh -n scripts/dev/test-dotnet.sh
	@sh -n scripts/dev/github-projects.sh
	@sh -n scripts/dev/test-github-projects.sh
	@sh -n scripts/dev/local-e2e-smoke.sh
	@sh -n scripts/dev/local-compose-check.sh

github-projects-script-test:
	@sh scripts/dev/test-github-projects.sh

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
