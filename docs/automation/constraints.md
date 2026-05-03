# Agent Constraints

- Do not add, print, commit, or infer secrets, tokens, private keys, `.env`,
  Terraform state, or kubeconfigs.
- Do not perform paid Azure actions without explicit approval.
- Do not claim production readiness.
- Do not introduce non-Azure cloud services into the target architecture.
- Do not add `codex-workflows`, `.codex/agents`, or `.agents/skills` unless the
  user explicitly asks.
- Do not change standard LICENSE text.
- Do not implement behavior unless the task explicitly asks for implementation.
- Do not pollute human docs with agent chatter.
- Keep automation docs compact; put durable explanations in architecture,
  product, ADR, or standards docs.
