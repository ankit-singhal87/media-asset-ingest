# Decision Log

This file records short operational decisions that do not need a full ADR.
Architecture-significant decisions belong in `docs/adr`.

## Decisions

- Use `docs/automation` for low-token agent context.
- Avoid `codex-workflows` for now; use Superpowers and repo-local automation docs.
- Keep `.codex/agents` and `.agents/skills` out of the repo unless explicitly adopted later.
- Use Linux as the supported development workstation assumption.

## Update Rule

Agents must add an entry here when a decision affects workflow, tooling,
documentation structure, or development process but does not justify a full ADR.

