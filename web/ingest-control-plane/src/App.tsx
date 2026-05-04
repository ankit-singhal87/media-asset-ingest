import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import mermaid from "mermaid";

import {
  buildMermaidFlowchart,
  mockedWorkflowGraph,
  summarizeStatuses,
  toMermaidNodeId,
  type WorkflowGraph,
  type WorkflowNode,
  type WorkflowNodeDetails
} from "./workflowGraph";

type LocalWatcherStatus = "idle" | "starting" | "watching" | "error";
type PackageStatusLoadState = "loading" | "ready" | "error";
type WorkflowGraphLoadState = "ready" | "loading" | "error";
type WorkflowNodeDetailsLoadState = "idle" | "loading" | "ready" | "error";

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

async function fetchWorkflowNodeDetails(workflowInstanceId: string, nodeId: string) {
  const response = await fetch(
    `/api/workflows/${encodeURIComponent(workflowInstanceId)}/nodes/${encodeURIComponent(nodeId)}`
  );

  if (!response.ok) {
    throw new Error(`Workflow node details failed with ${response.status}`);
  }

  return (await response.json()) as WorkflowNodeDetails;
}

function formatStatus(status: WorkflowNode["status"]) {
  return status.toLowerCase();
}

function formatNodeKind(kind: WorkflowNode["kind"] | string) {
  return kind
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/^./, (firstCharacter) => firstCharacter.toUpperCase());
}

function formatNodeReference(node: WorkflowNode) {
  if (node.workItemId) {
    return `Work item ${node.workItemId}`;
  }

  if (node.childWorkflowInstanceId) {
    return `Child workflow ${node.childWorkflowInstanceId}`;
  }

  return `Node ${node.nodeId}`;
}

function formatUpdatedAt(updatedAt: string) {
  return new Intl.DateTimeFormat("en", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC"
  }).format(new Date(updatedAt));
}

function NodeDetailsPanel({
  selectedNode,
  details,
  loadState
}: {
  selectedNode?: WorkflowNode;
  details?: WorkflowNodeDetails;
  loadState: WorkflowNodeDetailsLoadState;
}) {
  return (
    <section className="node-details-panel" aria-label="selected workflow node details">
      <div className="section-heading">
        <div>
          <h2>{selectedNode?.displayName ?? "Node details"}</h2>
          {selectedNode && <span>{selectedNode.nodeId}</span>}
        </div>
        {selectedNode && (
          <span className="node-details-status">{selectedNode.status}</span>
        )}
      </div>
      {!selectedNode && <p>Select a workflow node to inspect details.</p>}
      {selectedNode && loadState === "loading" && (
        <p role="status">Loading node details...</p>
      )}
      {selectedNode && loadState === "error" && (
        <p role="status">Node details unavailable</p>
      )}
      {selectedNode && loadState === "ready" && details && (
        <>
          <dl className="node-details-metadata" aria-label="selected node metadata">
            <div>
              <dt>Status</dt>
              <dd>{selectedNode.status}</dd>
            </div>
            <div>
              <dt>Kind</dt>
              <dd>{formatNodeKind(selectedNode.kind)}</dd>
            </div>
            <div>
              <dt>Reference</dt>
              <dd>{formatNodeReference(selectedNode)}</dd>
            </div>
            <div>
              <dt>Workflow</dt>
              <dd>{selectedNode.workflowInstanceId}</dd>
            </div>
          </dl>
          <div className="node-details-grid">
            <div>
              <h3>Timeline</h3>
              {details.timeline.length === 0 ? (
                <p>No timeline entries recorded for this node yet.</p>
              ) : (
                <ol className="node-detail-rows">
                  {details.timeline.map((entry) => (
                    <li key={`${entry.occurredAt}-${entry.message}`}>
                      <time dateTime={entry.occurredAt}>{formatUpdatedAt(entry.occurredAt)}</time>
                      <strong>{entry.status}</strong>
                      <span>{entry.message}</span>
                      <code>{entry.correlationId}</code>
                    </li>
                  ))}
                </ol>
              )}
            </div>
            <div>
              <h3>Logs</h3>
              {details.logs.length === 0 ? (
                <p>No log entries recorded for this node yet.</p>
              ) : (
                <ol className="node-detail-rows">
                  {details.logs.map((entry) => (
                    <li key={`${entry.occurredAt}-${entry.message}`}>
                      <time dateTime={entry.occurredAt}>{formatUpdatedAt(entry.occurredAt)}</time>
                      <strong>{entry.level}</strong>
                      <span>{entry.message}</span>
                      <code>{entry.correlationId}</code>
                      {entry.traceId && <code>{entry.traceId}</code>}
                      {entry.spanId && <code>{entry.spanId}</code>}
                    </li>
                  ))}
                </ol>
              )}
            </div>
          </div>
        </>
      )}
    </section>
  );
}

function MermaidWorkflowDiagram({
  graph,
  selectedNodeId,
  onNodeActivate
}: {
  graph: WorkflowGraph;
  selectedNodeId?: string;
  onNodeActivate: (node: WorkflowNode) => void;
}) {
  const [diagramSvg, setDiagramSvg] = useState("");
  const diagramRef = useRef<HTMLDivElement>(null);
  const diagramSyntax = useMemo(() => buildMermaidFlowchart(graph), [graph]);

  useEffect(() => {
    let cancelled = false;
    setDiagramSvg("");

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

  useEffect(() => {
    const diagramElement = diagramRef.current;

    if (!diagramElement || !diagramSvg) {
      return undefined;
    }

    const cleanups = graph.nodes.flatMap((node) => {
      const mermaidId = toMermaidNodeId(node.nodeId);
      const svgNodes = Array.from(
        diagramElement.querySelectorAll<SVGGElement>(`[id^="flowchart-${mermaidId}-"]`)
      );

      return svgNodes.map((svgNode) => {
        const activateNode = () => onNodeActivate(node);
        const activateNodeFromKeyboard = (event: KeyboardEvent) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            activateNode();
          }
        };

        svgNode.classList.add("workflow-diagram__node");
        svgNode.setAttribute("role", "button");
        svgNode.setAttribute("tabindex", "0");
        svgNode.setAttribute(
          "aria-label",
          `${node.displayName} ${formatStatus(node.status)} ${formatNodeKind(node.kind)}`
        );
        svgNode.setAttribute("aria-pressed", String(selectedNodeId === node.nodeId));
        svgNode.addEventListener("click", activateNode);
        svgNode.addEventListener("keydown", activateNodeFromKeyboard);

        return () => {
          svgNode.removeEventListener("click", activateNode);
          svgNode.removeEventListener("keydown", activateNodeFromKeyboard);
        };
      });
    });

    return () => {
      cleanups.forEach((cleanup) => cleanup());
    };
  }, [diagramSvg, graph.nodes, onNodeActivate, selectedNodeId]);

  if (!diagramSvg) {
    return <p role="status">Rendering workflow diagram...</p>;
  }

  return (
    <div
      ref={diagramRef}
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
  const [selectedWorkflowInstanceId, setSelectedWorkflowInstanceId] = useState<string>();
  const [graph, setGraph] = useState<WorkflowGraph>(mockedWorkflowGraph);
  const [workflowNavigationStack, setWorkflowNavigationStack] = useState<WorkflowGraph[]>([]);
  const [selectedNode, setSelectedNode] = useState<WorkflowNode>();
  const [workflowNodeDetailsLoadState, setWorkflowNodeDetailsLoadState] =
    useState<WorkflowNodeDetailsLoadState>("idle");
  const [workflowNodeDetails, setWorkflowNodeDetails] = useState<WorkflowNodeDetails>();
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

  const clearNodeDetails = useCallback(() => {
    setSelectedNode(undefined);
    setWorkflowNodeDetails(undefined);
    setWorkflowNodeDetailsLoadState("idle");
  }, []);

  const loadWorkflowGraph = useCallback(async (workflowInstanceId: string) => {
    setWorkflowGraphLoadState("loading");
    clearNodeDetails();

    try {
      const workflowGraph = await fetchWorkflowGraph(workflowInstanceId);

      setGraph(workflowGraph);
      setWorkflowGraphLoadState("ready");
    } catch {
      setWorkflowGraphLoadState("error");
    }
  }, [clearNodeDetails]);

  const loadWorkflowNodeDetails = useCallback(async (node: WorkflowNode) => {
    setSelectedNode(node);
    setWorkflowNodeDetails(undefined);
    setWorkflowNodeDetailsLoadState("loading");

    try {
      const details = await fetchWorkflowNodeDetails(node.workflowInstanceId, node.nodeId);

      setWorkflowNodeDetails(details);
      setWorkflowNodeDetailsLoadState("ready");
    } catch {
      setWorkflowNodeDetailsLoadState("error");
    }
  }, []);

  const activateWorkflowNode = useCallback((node: WorkflowNode) => {
    if (node.kind === "ChildWorkflow" && node.childWorkflowInstanceId) {
      setWorkflowNavigationStack((navigationStack) => [...navigationStack, graph]);
      void loadWorkflowGraph(node.childWorkflowInstanceId);
      return;
    }

    void loadWorkflowNodeDetails(node);
  }, [graph, loadWorkflowGraph, loadWorkflowNodeDetails]);

  const navigateToParentWorkflow = useCallback(() => {
    setWorkflowNavigationStack((navigationStack) => {
      const parentGraph = navigationStack.at(-1);

      if (!parentGraph) {
        return navigationStack;
      }

      setGraph(parentGraph);
      setWorkflowGraphLoadState("ready");
      clearNodeDetails();

      return navigationStack.slice(0, -1);
    });
  }, [clearNodeDetails]);

  const selectPackageWorkflow = useCallback((workflowInstanceId: string) => {
    clearNodeDetails();
    setWorkflowNavigationStack([]);

    setSelectedWorkflowInstanceId(workflowInstanceId);
  }, [clearNodeDetails]);

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
    if (
      packageStatusLoadState === "ready" &&
      !selectedWorkflowInstanceId &&
      packageStatuses[0]?.workflowInstanceId
    ) {
      setSelectedWorkflowInstanceId(packageStatuses[0].workflowInstanceId);
    }
  }, [packageStatusLoadState, packageStatuses, selectedWorkflowInstanceId]);

  useEffect(() => {
    if (packageStatusLoadState === "ready" && selectedWorkflowInstanceId) {
      void loadWorkflowGraph(selectedWorkflowInstanceId);
    }
  }, [loadWorkflowGraph, packageStatusLoadState, selectedWorkflowInstanceId]);

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
                <button
                  type="button"
                  className="package-status-button"
                  aria-label={`Select package ${packageStatus.packageId} workflow ${packageStatus.workflowInstanceId}`}
                  aria-pressed={
                    selectedWorkflowInstanceId === packageStatus.workflowInstanceId
                  }
                  onClick={() => selectPackageWorkflow(packageStatus.workflowInstanceId)}
                >
                  <span className="package-status-item__state">
                    {packageStatus.status}
                  </span>
                  <strong>{packageStatus.packageId}</strong>
                  <code>{packageStatus.workflowInstanceId}</code>
                  <span>Updated {formatUpdatedAt(packageStatus.updatedAt)}</span>
                </button>
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
          <div>
            <h2 id="graph-heading">{graph.workflowName}</h2>
            {graph.parentWorkflowInstanceId && (
              <span>Child workflow of {graph.parentWorkflowInstanceId}</span>
            )}
          </div>
          <span>Mermaid workflow graph</span>
        </div>
        {workflowGraphLoadState === "loading" && (
          <p role="status">Loading workflow graph...</p>
        )}
        {workflowGraphLoadState === "error" && (
          <p role="status">Workflow graph unavailable</p>
        )}
        <div className="workflow-diagram">
          <MermaidWorkflowDiagram
            graph={graph}
            selectedNodeId={selectedNode?.nodeId}
            onNodeActivate={activateWorkflowNode}
          />
        </div>
        <div className="workflow-diagram-actions">
          {workflowNavigationStack.length > 0 && (
            <button
              type="button"
              className="workflow-back-button"
              onClick={navigateToParentWorkflow}
            >
              Back to {workflowNavigationStack.at(-1)?.workflowName}
            </button>
          )}
          <label>
            <span>Inspect workflow diagram node</span>
            <select
              aria-label="Inspect workflow diagram node"
              value={selectedNode?.nodeId ?? ""}
              onChange={(event) => {
                const node = graph.nodes.find(
                  (candidate) => candidate.nodeId === event.target.value
                );

                if (node) {
                  activateWorkflowNode(node);
                }
              }}
            >
              <option value="">Choose a node</option>
              {graph.nodes.map((node) => (
                <option key={node.nodeId} value={node.nodeId}>
                  {node.displayName} - {node.status} - {formatNodeReference(node)}
                </option>
              ))}
            </select>
          </label>
        </div>
      </section>

      <NodeDetailsPanel
        selectedNode={selectedNode}
        details={workflowNodeDetails}
        loadState={workflowNodeDetailsLoadState}
      />
    </main>
  );
}
