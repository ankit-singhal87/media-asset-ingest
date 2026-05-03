# Validation Matrix

| Change type | Minimal validation | Stronger validation | Docker? | Cloud? | Cost |
| --- | --- | --- | --- | --- | --- |
| Docs-only change | `make docs-check` | `make docs-fix` before committing when formatting/link checks fail | no | no | cheap |
| Architecture/ADR change | targeted term search for contradictory decisions | review affected ADRs and architecture docs together | no | no | cheap |
| Automation-doc change | `make validate` | read `AGENTS.md`, task workflow, and automation docs for consistency | no | no | cheap |
| Standards change | `make validate` | review affected automation docs and task workflow | no | no | cheap |
| Tooling change | `make validate` and `make check-tools` when host tools are expected | `make print-install-tools` review | no | no | cheap |
| GitHub tracker change | `make github-project-summary` and `make github-project-hierarchy` | `make github-project-active` plus targeted `gh issue view` or dependency checks | no | no | cheap |
| .NET code change | focused unit test command once solution exists | full test suite once available | no by default | no | moderate |
| Docker/Kubernetes change | relevant local smoke test once scripts exist | full local runtime validation | yes | no | moderate |
| Azure deployment change | static manifest/terraform validation only | manual cloud validation after approval | maybe | yes | paid/approval |

Do not run expensive Docker or cloud validation for docs-only edits.

Agents must report validation commands and outcomes before claiming completion.
