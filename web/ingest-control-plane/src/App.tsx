import { useState } from "react";

import { mockedWorkflowGraph, summarizeStatuses, type WorkflowNode } from "./workflowGraph";

type LocalWatcherStatus = "idle" | "starting" | "watching" | "error";

const progressedStatuses = new Set<WorkflowNode["status"]>([
  "Running",
  "Succeeded",
  "Failed",
  "Skipped",
  "Cancelled"
]);

const statusOrder: WorkflowNode["status"][] = [
  "Pending",
  "Running",
  "Failed",
  "Waiting",
  "Succeeded"
];

function formatStatus(status: WorkflowNode["status"]) {
  return status.toLowerCase();
}

function NodeCard({ node }: { node: WorkflowNode }) {
  return (
    <li
      className={`workflow-node workflow-node--${formatStatus(node.status)}`}
      aria-label={`${node.displayName} ${formatStatus(node.status)}`}
    >
      <span className="workflow-node__status">{node.status}</span>
      <strong>{node.displayName}</strong>
      <span>{node.kind}</span>
      <code>{node.workItemId ?? node.childWorkflowInstanceId ?? node.nodeId}</code>
    </li>
  );
}

export function App() {
  const graph = mockedWorkflowGraph;
  const [localWatcherStatus, setLocalWatcherStatus] =
    useState<LocalWatcherStatus>("idle");
  const statusSummary = summarizeStatuses(graph.nodes);
  const progressedNodeCount = graph.nodes.filter((node) =>
    progressedStatuses.has(node.status)
  ).length;

  async function startLocalIngest() {
    setLocalWatcherStatus("starting");

    try {
      const response = await fetch("/api/ingest/start", { method: "POST" });

      if (!response.ok) {
        throw new Error(`Ingest start failed with ${response.status}`);
      }

      setLocalWatcherStatus("watching");
    } catch {
      setLocalWatcherStatus("error");
    }
  }

  return (
    <main className="control-plane-shell">
      <header className="control-plane-header">
        <div>
          <p className="eyebrow">Workflow graph</p>
          <h1>Workflow Control Plane</h1>
        </div>
        <dl className="workflow-metadata" aria-label="workflow metadata">
          <div>
            <dt>Package</dt>
            <dd>Package {graph.packageId}</dd>
          </div>
          <div>
            <dt>Workflow</dt>
            <dd>{graph.workflowInstanceId}</dd>
          </div>
        </dl>
      </header>

      <section className="local-ingest-panel" aria-label="local ingest watcher">
        <div>
          <h2>Local ingest</h2>
          <p>Local watcher: {localWatcherStatus}</p>
        </div>
        <button
          type="button"
          className="start-ingest-button"
          disabled={localWatcherStatus === "starting"}
          onClick={startLocalIngest}
        >
          Start ingest
        </button>
      </section>

      <section className="summary-band" aria-label="workflow progress summary">
        <strong>
          {progressedNodeCount} of {graph.nodes.length} nodes progressed
        </strong>
        <span>{graph.edges.length} graph edges</span>
        {statusOrder.map((status) => (
          <span key={status}>
            {formatStatus(status)}: {statusSummary[status]}
          </span>
        ))}
      </section>

      <section className="graph-section" aria-labelledby="graph-heading">
        <div className="section-heading">
          <h2 id="graph-heading">{graph.workflowName}</h2>
          <span>Mocked workflow graph data</span>
        </div>
        <ol className="workflow-graph" aria-label="mocked workflow graph">
          {graph.nodes.map((node) => (
            <NodeCard key={node.nodeId} node={node} />
          ))}
        </ol>
      </section>
    </main>
  );
}
