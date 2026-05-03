# Guardrails

- Keep diffs focused; avoid broad rewrites.
- Avoid unrelated formatting churn.
- Do not upgrade or add dependencies without a task-specific reason and approval where needed.
- Do not generate or commit secrets.
- Do not invent production-readiness claims.
- Do not add non-Azure cloud dependencies to the target architecture.
- Do not add large docs without updating the relevant index or read order.
- Keep human and agent documentation lanes separate.
- Preserve useful existing docs; move or summarize only when clearly misplaced, stale, duplicate, or unsafe.
- Prefer durable business state over log scraping for UI behavior.
- Use `workflowInstanceId`, `packageId`, `fileId`, `workItemId`, and `nodeId`
  consistently when designing logs, traces, and UI projections.
- Follow BDD/TDD for behavior changes unless the user approves an exception.
- Keep domain behavior independent of Dapr, Azure Service Bus, PostgreSQL, and
  UI infrastructure details.
- Update status, backlog, milestone, standards, tooling, and automation docs
  when a task changes their meaning.
- Update GitHub Projects when issue, milestone, task, bug, dependency, worktree,
  or PR state changes.
- Do not duplicate GitHub-native relationships in issue descriptions.
- Update `README.md` quickstart when setup, tools, commands, validation, or
  local runtime behavior changes.
- If a new required developer tool is introduced, update tooling standards,
  detection, installation guidance, commands, and validation before completion.
