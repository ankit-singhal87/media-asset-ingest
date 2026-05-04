export type WorkflowNodeStatus =
  | "Pending"
  | "Queued"
  | "Running"
  | "Succeeded"
  | "Failed"
  | "Waiting"
  | "Skipped"
  | "Cancelled";

export type WorkflowNodeKind =
  | "WorkflowStep"
  | "Activity"
  | "ChildWorkflow"
  | "WorkItem";

export type WorkflowNode = {
  nodeId: string;
  displayName: string;
  kind: WorkflowNodeKind;
  status: WorkflowNodeStatus;
  workflowInstanceId: string;
  packageId: string;
  workItemId?: string;
  childWorkflowInstanceId?: string;
};

export type WorkflowEdge = {
  edgeId: string;
  sourceNodeId: string;
  targetNodeId: string;
};

export type WorkflowGraph = {
  workflowInstanceId: string;
  workflowName: string;
  packageId: string;
  parentWorkflowInstanceId?: string;
  nodes: WorkflowNode[];
  edges: WorkflowEdge[];
};

export type WorkflowTimelineEntry = {
  occurredAt: string;
  status: WorkflowNodeStatus;
  message: string;
  correlationId: string;
};

export type WorkflowNodeLogEntry = {
  occurredAt: string;
  level: string;
  message: string;
  correlationId: string;
  traceId?: string | null;
  spanId?: string | null;
};

export type WorkflowNodeDetails = {
  workflowInstanceId: string;
  nodeId: string;
  timeline: WorkflowTimelineEntry[];
  logs: WorkflowNodeLogEntry[];
};

type WorkflowStatusStyle = {
  fill: string;
  stroke: string;
  text: string;
};

export const workflowStatusPalette: Record<WorkflowNodeStatus, WorkflowStatusStyle> = {
  Pending: { fill: "#e5eaf0", stroke: "#7b8794", text: "#344054" },
  Queued: { fill: "#e5eaf0", stroke: "#7b8794", text: "#344054" },
  Running: { fill: "#dbeafe", stroke: "#2563eb", text: "#1e3a8a" },
  Succeeded: { fill: "#d9f0e4", stroke: "#18865b", text: "#07583d" },
  Failed: { fill: "#ffedd5", stroke: "#c2410c", text: "#9a3412" },
  Waiting: { fill: "#ede9fe", stroke: "#8b5cf6", text: "#5b21b6" },
  Skipped: { fill: "#eef2f5", stroke: "#8a94a3", text: "#4b5563" },
  Cancelled: { fill: "#eef2f5", stroke: "#8a94a3", text: "#4b5563" }
};

export const mockedWorkflowGraph: WorkflowGraph = {
  workflowInstanceId: "wf-pkg-2026-05-03-001",
  workflowName: "Package ingest workflow",
  packageId: "PKG-2026-05-03-001",
  nodes: [
    {
      nodeId: "manifest",
      displayName: "Manifest detected",
      kind: "WorkflowStep",
      status: "Succeeded",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001"
    },
    {
      nodeId: "scan",
      displayName: "Scan package files",
      kind: "Activity",
      status: "Succeeded",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001"
    },
    {
      nodeId: "classify",
      displayName: "Classify package",
      kind: "Activity",
      status: "Running",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001"
    },
    {
      nodeId: "video",
      displayName: "Source video work item",
      kind: "WorkItem",
      status: "Succeeded",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: "work-video-master"
    },
    {
      nodeId: "proxy",
      displayName: "Proxy workflow",
      kind: "ChildWorkflow",
      status: "Waiting",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001",
      childWorkflowInstanceId: "wf-proxy-2026-05-03-001"
    },
    {
      nodeId: "audio",
      displayName: "Audio essence",
      kind: "WorkItem",
      status: "Pending",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: "work-audio-main"
    },
    {
      nodeId: "subtitle",
      displayName: "Subtitle sidecar",
      kind: "WorkItem",
      status: "Failed",
      workflowInstanceId: "wf-pkg-2026-05-03-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: "work-subtitle-en"
    }
  ],
  edges: [
    { edgeId: "manifest-scan", sourceNodeId: "manifest", targetNodeId: "scan" },
    { edgeId: "scan-classify", sourceNodeId: "scan", targetNodeId: "classify" },
    { edgeId: "classify-video", sourceNodeId: "classify", targetNodeId: "video" },
    { edgeId: "classify-proxy", sourceNodeId: "classify", targetNodeId: "proxy" },
    { edgeId: "classify-audio", sourceNodeId: "classify", targetNodeId: "audio" },
    { edgeId: "classify-subtitle", sourceNodeId: "classify", targetNodeId: "subtitle" }
  ]
};

export function summarizeStatuses(nodes: WorkflowNode[]) {
  return nodes.reduce<Record<WorkflowNodeStatus, number>>(
    (summary, node) => {
      summary[node.status] += 1;
      return summary;
    },
    {
      Pending: 0,
      Queued: 0,
      Running: 0,
      Succeeded: 0,
      Failed: 0,
      Waiting: 0,
      Skipped: 0,
      Cancelled: 0
    }
  );
}

export function buildMermaidFlowchart(graph: WorkflowGraph) {
  const nodeIdMap = new Map(
    graph.nodes.map((node) => [node.nodeId, toMermaidNodeId(node.nodeId)])
  );
  const lines = ["flowchart LR"];

  for (const node of graph.nodes) {
    const mermaidId = nodeIdMap.get(node.nodeId) ?? toMermaidNodeId(node.nodeId);
    lines.push(
      `  ${mermaidId}["${escapeMermaidLabel(node.displayName)}"]:::${formatStatusClass(node.status)}`
    );
  }

  graph.edges.forEach((edge) => {
    const sourceId = nodeIdMap.get(edge.sourceNodeId) ?? toMermaidNodeId(edge.sourceNodeId);
    const targetId = nodeIdMap.get(edge.targetNodeId) ?? toMermaidNodeId(edge.targetNodeId);
    lines.push(`  ${sourceId} --> ${targetId}`);
  });

  for (const [status, style] of Object.entries(workflowStatusPalette)) {
    lines.push(
      `  classDef ${formatStatusClass(status as WorkflowNodeStatus)} fill:${style.fill},stroke:${style.stroke},color:${style.text},stroke-width:2px`
    );
  }

  graph.edges.forEach((edge, index) => {
    const target = graph.nodes.find((node) => node.nodeId === edge.targetNodeId);
    const style = workflowStatusPalette[target?.status ?? "Pending"];
    lines.push(`  linkStyle ${index} stroke:${style.stroke},stroke-width:2px`);
  });

  return lines.join("\n");
}

export function toMermaidNodeId(nodeId: string) {
  const sanitized = nodeId.replace(/[^A-Za-z0-9_]/g, "_");

  return /^[A-Za-z_]/.test(sanitized) ? sanitized : `node_${sanitized}`;
}

function escapeMermaidLabel(label: string) {
  return label
    .replace(/"/g, "#quot;")
    .replace(/\[/g, "#91;")
    .replace(/\]/g, "#93;");
}

function formatStatusClass(status: WorkflowNodeStatus) {
  return status.toLowerCase();
}
