.PHONY: help check-tools install-tools print-install-tools validate docs-check scripts-check

help:
	@printf '%s\n' "Available targets:"
	@printf '%s\n' "  make check-tools         Verify required Linux development tools"
	@printf '%s\n' "  make install-tools       Install supported Linux development tools"
	@printf '%s\n' "  make print-install-tools Print manual installation commands"
	@printf '%s\n' "  make validate            Run cheap repository validation"
	@printf '%s\n' "  make docs-check          Check docs for unfinished placeholders"
	@printf '%s\n' "  make scripts-check       Syntax-check shell scripts"

check-tools:
	@sh scripts/dev/check-tools.sh

install-tools:
	@sh scripts/dev/install-tools.sh

print-install-tools:
	@sh scripts/dev/install-tools.sh --print-only

validate: docs-check scripts-check

docs-check:
	@npm run docs:check

scripts-check:
	@sh -n scripts/dev/check-tools.sh
	@sh -n scripts/dev/install-tools.sh

