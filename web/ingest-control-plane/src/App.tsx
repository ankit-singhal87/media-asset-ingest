import { useCallback, useEffect, useMemo, useState } from "react";
import mermaid from "mermaid";

import {
  buildMermaidFlowchart,
  mockedWorkflowGraph,
  summarizeStatuses,
  type WorkflowGraph,
  type WorkflowNode
} from "./workflowGraph";

type LocalWatcherStatus = "idle" | "starting" | "watching" | "error";
type PackageStatusLoadState = "loading" | "ready" | "error";
type WorkflowGraphLoadState = "ready" | "loading" | "error";

type IngestPackageStatus = {
  packageId: string;
  workflowInstanceId: string;
  status: string;
  updatedAt: string;
};

type IngestStatusResponse = {
  packages: IngestPackageStatus[];
};

const progressedStatuses = new Set<WorkflowNode["status"]>([
  "Running",
  "Succeeded",
  "Failed",
  "Skipped",
  "Cancelled"
]);

const statusOrder: WorkflowNode["status"][] = [
  "Pending",
  "Queued",
  "Running",
  "Failed",
  "Waiting",
  "Succeeded",
  "Skipped",
  "Cancelled"
];
const packageStatusRefreshIntervalMs = 5000;

async function fetchPackageStatuses() {
  const response = await fetch("/api/ingest/status");

  if (!response.ok) {
    throw new Error(`Ingest status failed with ${response.status}`);
  }

  const statusResponse = (await response.json()) as IngestStatusResponse;

  return statusResponse.packages;
}

async function fetchWorkflowGraph(workflowInstanceId: string) {
  const response = await fetch(`/api/workflows/${encodeURIComponent(workflowInstanceId)}/graph`);

  if (!response.ok) {
    throw new Error(`Workflow graph failed with ${response.status}`);
  }

  return (await response.json()) as WorkflowGraph;
}

function formatStatus(status: WorkflowNode["status"]) {
  return status.toLowerCase();
}

function formatUpdatedAt(updatedAt: string) {
  return new Intl.DateTimeFormat("en", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC"
  }).format(new Date(updatedAt));
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

function MermaidWorkflowDiagram({ graph }: { graph: WorkflowGraph }) {
  const [diagramSvg, setDiagramSvg] = useState("");
  const diagramSyntax = useMemo(() => buildMermaidFlowchart(graph), [graph]);

  useEffect(() => {
    let cancelled = false;

    mermaid.initialize({
      startOnLoad: false,
      securityLevel: "strict",
      theme: "base"
    });

    void mermaid
      .render(`workflow-graph-${graph.workflowInstanceId.replace(/[^A-Za-z0-9_-]/g, "-")}`, diagramSyntax)
      .then(({ svg }) => {
        if (!cancelled) {
          setDiagramSvg(svg);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [diagramSyntax, graph.workflowInstanceId]);

  if (!diagramSvg) {
    return <p role="status">Rendering workflow diagram...</p>;
  }

  return (
    <div
      role="img"
      aria-label="workflow diagram"
      className="workflow-diagram__svg"
      dangerouslySetInnerHTML={{ __html: diagramSvg }}
    />
  );
}

export function App() {
  const [localWatcherStatus, setLocalWatcherStatus] =
    useState<LocalWatcherStatus>("idle");
  const [packageStatusLoadState, setPackageStatusLoadState] =
    useState<PackageStatusLoadState>("loading");
  const [workflowGraphLoadState, setWorkflowGraphLoadState] =
    useState<WorkflowGraphLoadState>("ready");
  const [packageStatuses, setPackageStatuses] = useState<IngestPackageStatus[]>([]);
  const [graph, setGraph] = useState<WorkflowGraph>(mockedWorkflowGraph);
  const statusSummary = summarizeStatuses(graph.nodes);
  const progressedNodeCount = graph.nodes.filter((node) =>
    progressedStatuses.has(node.status)
  ).length;

  const loadPackageStatuses = useCallback(async () => {
    try {
      const packages = await fetchPackageStatuses();

      setPackageStatuses(packages);
      setPackageStatusLoadState("ready");
    } catch {
      setPackageStatusLoadState("error");
    }
  }, []);

  const loadWorkflowGraph = useCallback(async (workflowInstanceId: string) => {
    setWorkflowGraphLoadState("loading");

    try {
      const workflowGraph = await fetchWorkflowGraph(workflowInstanceId);

      setGraph(workflowGraph);
      setWorkflowGraphLoadState("ready");
    } catch {
      setWorkflowGraphLoadState("error");
    }
  }, []);

  useEffect(() => {
    void loadPackageStatuses();
    const refreshIntervalId = window.setInterval(
      () => void loadPackageStatuses(),
      packageStatusRefreshIntervalMs
    );

    return () => {
      window.clearInterval(refreshIntervalId);
    };
  }, [loadPackageStatuses]);

  useEffect(() => {
    const selectedWorkflowInstanceId = packageStatuses[0]?.workflowInstanceId;

    if (packageStatusLoadState === "ready" && selectedWorkflowInstanceId) {
      void loadWorkflowGraph(selectedWorkflowInstanceId);
    }
  }, [loadWorkflowGraph, packageStatusLoadState, packageStatuses]);

  async function startLocalIngest() {
    setLocalWatcherStatus("starting");

    try {
      const response = await fetch("/api/ingest/start", { method: "POST" });

      if (!response.ok) {
        throw new Error(`Ingest start failed with ${response.status}`);
      }

      setLocalWatcherStatus("watching");
      void loadPackageStatuses();
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

      <section className="package-status-panel" aria-label="real package status">
        <div className="section-heading">
          <h2>Package status</h2>
          <span>Live API data</span>
        </div>
        {packageStatusLoadState === "loading" && <p>Loading package status...</p>}
        {packageStatusLoadState === "error" && (
          <p role="status">Package status unavailable</p>
        )}
        {packageStatusLoadState === "ready" && packageStatuses.length === 0 && (
          <p>No packages reported yet</p>
        )}
        {packageStatusLoadState === "ready" && packageStatuses.length > 0 && (
          <ol className="package-status-list">
            {packageStatuses.map((packageStatus) => (
              <li key={packageStatus.packageId} className="package-status-item">
                <span className="package-status-item__state">
                  {packageStatus.status}
                </span>
                <strong>{packageStatus.packageId}</strong>
                <code>{packageStatus.workflowInstanceId}</code>
                <span>Updated {formatUpdatedAt(packageStatus.updatedAt)}</span>
              </li>
            ))}
          </ol>
        )}
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
          <span>Mermaid workflow graph</span>
        </div>
        {workflowGraphLoadState === "loading" && (
          <p role="status">Loading workflow graph...</p>
        )}
        {workflowGraphLoadState === "error" && (
          <p role="status">Workflow graph unavailable</p>
        )}
        <div className="workflow-diagram">
          <MermaidWorkflowDiagram graph={graph} />
        </div>
        <ol className="workflow-graph" aria-label="workflow graph node status">
          {graph.nodes.map((node) => (
            <NodeCard key={node.nodeId} node={node} />
          ))}
        </ol>
      </section>
    </main>
  );
}
