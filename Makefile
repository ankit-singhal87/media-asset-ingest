.PHONY: help check-tools install-tools print-install-tools validate docs-check docs-fix scripts-check github-projects-script-test github-project-check github-project-summary github-project-hierarchy github-project-active

help:
	@printf '%s\n' "Available targets:"
	@printf '%s\n' "  make check-tools         Verify required Linux development tools"
	@printf '%s\n' "  make install-tools       Install supported Linux development tools"
	@printf '%s\n' "  make print-install-tools Print manual installation commands"
	@printf '%s\n' "  make validate            Run cheap repository validation"
	@printf '%s\n' "  make docs-check          Check docs for unfinished placeholders"
	@printf '%s\n' "  make docs-fix            Apply safe docs formatting fixes"
	@printf '%s\n' "  make scripts-check       Syntax-check shell scripts"
	@printf '%s\n' "  make github-projects-script-test Test GitHub tracker helper wrappers"
	@printf '%s\n' "  make github-project-check     Verify GitHub CLI auth and project access"
	@printf '%s\n' "  make github-project-summary   Print GitHub tracker counts"
	@printf '%s\n' "  make github-project-hierarchy Print epic/story hierarchy"
	@printf '%s\n' "  make github-project-active    Print in-progress project items"

check-tools:
	@sh scripts/dev/check-tools.sh

install-tools:
	@sh scripts/dev/install-tools.sh

print-install-tools:
	@sh scripts/dev/install-tools.sh --print-only

validate: docs-check scripts-check github-projects-script-test

docs-check:
	@npm run docs:check

docs-fix:
	@npm run docs:fix

scripts-check:
	@sh -n scripts/dev/check-tools.sh
	@sh -n scripts/dev/install-tools.sh
	@sh -n scripts/dev/github-projects.sh
	@sh -n scripts/dev/test-github-projects.sh

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
