# Guardrails

- Keep diffs focused; avoid broad rewrites.
- Inspect diff names and stats before targeted patches; read full diffs only when names, stats, or targeted file diffs are insufficient.
- Avoid unrelated formatting churn.
- Do not upgrade or add dependencies without a task-specific reason and approval where needed.
- Do not generate or commit secrets.
- Do not invent production-readiness claims.
- Do not add non-Azure cloud dependencies to the target architecture.
- Do not add large docs without updating the relevant index or read order.
- Keep human and agent documentation lanes separate.
- Keep detailed ephemeral planning in ignored `.worktrees/state/` records. Commit only compact durable plans under `docs/plans`; do not use ignored or generated planning artifacts as durable repo docs.
- Use repo-relative paths in committed docs, scripts, handoffs, and local state
  records. Do not write workstation-specific absolute paths, home-directory
  shortcuts, or hypervisor/shared-folder paths unless the task is explicitly
  documenting a local environment problem. Absolute paths are acceptable only
  for external runtime locations such as temporary validation logs or documented
  container mount points.
- Preserve useful existing docs; move or summarize only when clearly misplaced, stale, duplicate, or unsafe.
- Prefer durable business state over log scraping for UI behavior.
- Use `workflowInstanceId`, `packageId`, `fileId`, `workItemId`, and `nodeId`
  consistently when designing logs, traces, and UI projections.
- Follow BDD/TDD for behavior changes unless the user approves an exception.
- Read each relevant workflow skill once at the start of a workflow, then rely on the compact automation docs unless switching workflow type or the skill details are unclear.
- Keep domain behavior independent of Dapr, Azure Service Bus, PostgreSQL, and
  UI infrastructure details.
- Update status, backlog, milestone, standards, tooling, and automation docs
  when a task changes their meaning.
- Update repo docs when milestone, task, bug, dependency, or status state
  changes. Keep worktree state in ignored local `.worktrees/state/` files.
- Update `README.md` quickstart when setup, tools, commands, validation, or
  local runtime behavior changes.
- If a new required developer tool is introduced, update tooling standards,
  detection, installation guidance, commands, and validation before completion.
