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
