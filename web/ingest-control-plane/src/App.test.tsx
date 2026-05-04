import { act, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { App } from "./App";

vi.mock("mermaid", () => ({
  default: {
    initialize: vi.fn(),
    render: vi.fn(async (_id: string, diagram: string) => ({
      svg: `<svg role="img" data-diagram="${encodeURIComponent(diagram)}"></svg>`
    }))
  }
}));

describe("workflow graph control plane", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it("renders the live workflow graph with Mermaid and enough state to inspect progress", async () => {
    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ packages: [] }), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      );

    render(<App />);

    expect(
      screen.getByRole("heading", { name: /workflow control plane/i })
    ).toBeInTheDocument();
    expect(screen.getByText(/package PKG-2026-05-03-001/i)).toBeInTheDocument();
    expect(screen.getByText(/5 of 7 nodes progressed/i)).toBeInTheDocument();

    const graph = await screen.findByRole("img", { name: /workflow diagram/i });

    expect(graph).toHaveClass("workflow-diagram__svg");
    expect(graph.innerHTML).toContain("<svg");
    expect(
      screen.getByRole("button", { name: /manifest detected succeeded/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /classify package running/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /proxy workflow waiting/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /audio essence pending/i })
    ).toBeInTheDocument();

    expect(screen.getByText(/pending: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/running: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/failed: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/waiting: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/queued: 0/i)).toBeInTheDocument();
    expect(screen.getByText(/skipped: 0/i)).toBeInTheDocument();
    expect(screen.getByText(/cancelled: 0/i)).toBeInTheDocument();
    expect(screen.getByText(/no packages reported yet/i)).toBeInTheDocument();
  });

  it("starts local ingest watching when the operator clicks Start ingest", async () => {
    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ packages: [] }), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(new Response(null, { status: 202 }))
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ packages: [] }), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      );

    render(<App />);

    expect(screen.getByText(/local watcher: idle/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /start ingest/i }));

    expect(screen.getByText(/local watcher: starting/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(fetchMock).toHaveBeenNthCalledWith(2, "/api/ingest/start", {
        method: "POST"
      });
    });
    expect(await screen.findByText(/local watcher: watching/i)).toBeInTheDocument();
    await waitFor(() => {
      expect(fetchMock).toHaveBeenNthCalledWith(3, "/api/ingest/status");
    });
  });

  it("shows real package status from the ingest status endpoint separately from the mocked graph", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            packages: [
              {
                packageId: "PKG-LOCAL-001",
                workflowInstanceId: "workflow-local-001",
                status: "Running",
                updatedAt: "2026-05-03T18:42:00Z"
              }
            ]
          }),
          {
            headers: { "Content-Type": "application/json" },
            status: 200
          }
        )
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      );

    render(<App />);

    const statusPanel = screen.getByRole("region", {
      name: /real package status/i
    });

    expect(await within(statusPanel).findByText("PKG-LOCAL-001")).toBeInTheDocument();
    expect(within(statusPanel).getByText("workflow-local-001")).toBeInTheDocument();
    expect(within(statusPanel).getByText("Running")).toBeInTheDocument();
    expect(within(statusPanel).getByText(/May 3, 2026/)).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith("/api/ingest/status");
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("/api/workflows/workflow-local-001/graph");
    });
    expect(await screen.findByRole("img", { name: /workflow diagram/i })).toBeInTheDocument();
  });

  it("loads node details when the operator selects a workflow graph node", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            packages: [
              {
                packageId: "PKG-LOCAL-001",
                workflowInstanceId: "workflow-local-001",
                status: "Running",
                updatedAt: "2026-05-03T18:42:00Z"
              }
            ]
          }),
          {
            headers: { "Content-Type": "application/json" },
            status: 200
          }
        )
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(classifyNodeDetailsResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      );

    render(<App />);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("/api/workflows/workflow-local-001/graph");
    });
    fireEvent.click(await screen.findByRole("button", { name: /classify package running/i }));

    const details = await screen.findByRole("region", {
      name: /selected workflow node details/i
    });

    expect(fetchMock).toHaveBeenCalledWith("/api/workflows/workflow-local-001/nodes/classify");
    expect(within(details).getByRole("heading", { name: /classify package/i })).toBeInTheDocument();
    expect(within(details).getAllByText("node-classify")).toHaveLength(2);
    expect(within(details).getByText(/Classification started/i)).toBeInTheDocument();
    expect(within(details).getByText(/Command runner accepted classify work/i)).toBeInTheDocument();
    expect(within(details).getByText(/trace-classify-001/i)).toBeInTheDocument();
  });

  it("keeps selected node details visible when package status refreshes for the same workflow", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            packages: [
              {
                packageId: "PKG-LOCAL-001",
                workflowInstanceId: "workflow-local-001",
                status: "Running",
                updatedAt: "2026-05-03T18:42:00Z"
              }
            ]
          }),
          {
            headers: { "Content-Type": "application/json" },
            status: 200
          }
        )
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(classifyNodeDetailsResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(new Response(null, { status: 202 }))
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            packages: [
              {
                packageId: "PKG-LOCAL-001",
                workflowInstanceId: "workflow-local-001",
                status: "Running",
                updatedAt: "2026-05-03T18:42:05Z"
              }
            ]
          }),
          {
            headers: { "Content-Type": "application/json" },
            status: 200
          }
        )
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      );

    render(<App />);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("/api/workflows/workflow-local-001/graph");
    });
    fireEvent.click(await screen.findByRole("button", { name: /classify package running/i }));

    const details = await screen.findByRole("region", {
      name: /selected workflow node details/i
    });

    expect(within(details).getByRole("heading", { name: /classify package/i })).toBeInTheDocument();
    expect(within(details).getByText(/Classification started/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /start ingest/i }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("/api/ingest/start", { method: "POST" });
    });
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("/api/workflows/workflow-local-001/graph");
    });

    expect(within(details).getByRole("heading", { name: /classify package/i })).toBeInTheDocument();
    expect(within(details).getByText(/Classification started/i)).toBeInTheDocument();
  });

  it("refreshes real package status while the operator watches ingest progress", async () => {
    vi.useFakeTimers();

    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ packages: [] }), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(
        new Response(
          JSON.stringify({
            packages: [
              {
                packageId: "PKG-LIVE-001",
                workflowInstanceId: "workflow-live-001",
                status: "Succeeded",
                updatedAt: "2026-05-03T18:45:00Z"
              }
            ]
          }),
          {
            headers: { "Content-Type": "application/json" },
            status: 200
          }
        )
      )
      .mockResolvedValueOnce(
        new Response(JSON.stringify(liveWorkflowGraphResponse), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
    );

    render(<App />);

    await act(async () => {
      await Promise.resolve();
    });

    expect(screen.getByText(/no packages reported yet/i)).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(5000);
    });

    const statusPanel = screen.getByRole("region", {
      name: /real package status/i
    });

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(within(statusPanel).getByText("PKG-LIVE-001")).toBeInTheDocument();
    expect(within(statusPanel).getByText("Succeeded")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenNthCalledWith(2, "/api/ingest/status");
  });

  it("shows an error status when local ingest start fails", async () => {
    vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(
        new Response(JSON.stringify({ packages: [] }), {
          headers: { "Content-Type": "application/json" },
          status: 200
        })
      )
      .mockResolvedValueOnce(new Response(null, { status: 500 }));

    render(<App />);

    fireEvent.click(screen.getByRole("button", { name: /start ingest/i }));

    expect(await screen.findByText(/local watcher: error/i)).toBeInTheDocument();
  });
});

const liveWorkflowGraphResponse = {
  workflowInstanceId: "workflow-local-001",
  workflowName: "Package ingest workflow",
  packageId: "PKG-2026-05-03-001",
  parentWorkflowInstanceId: null,
  nodes: [
    {
      nodeId: "manifest",
      displayName: "Manifest detected",
      kind: "WorkflowStep",
      status: "Succeeded",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: null,
      childWorkflowInstanceId: null
    },
    {
      nodeId: "classify",
      displayName: "Classify package",
      kind: "Activity",
      status: "Running",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: null,
      childWorkflowInstanceId: null
    },
    {
      nodeId: "proxy",
      displayName: "Proxy workflow",
      kind: "ChildWorkflow",
      status: "Waiting",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: null,
      childWorkflowInstanceId: "workflow-proxy-001"
    },
    {
      nodeId: "audio",
      displayName: "Audio essence",
      kind: "WorkItem",
      status: "Pending",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: "audio-001",
      childWorkflowInstanceId: null
    },
    {
      nodeId: "subtitle",
      displayName: "Subtitle sidecar",
      kind: "WorkItem",
      status: "Failed",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: "subtitle-001",
      childWorkflowInstanceId: null
    },
    {
      nodeId: "video",
      displayName: "Source video work item",
      kind: "WorkItem",
      status: "Succeeded",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: "video-001",
      childWorkflowInstanceId: null
    },
    {
      nodeId: "done",
      displayName: "Done marker reconciliation",
      kind: "WorkflowStep",
      status: "Succeeded",
      workflowInstanceId: "workflow-local-001",
      packageId: "PKG-2026-05-03-001",
      workItemId: null,
      childWorkflowInstanceId: null
    }
  ],
  edges: [
    { edgeId: "manifest-classify", sourceNodeId: "manifest", targetNodeId: "classify" },
    { edgeId: "classify-proxy", sourceNodeId: "classify", targetNodeId: "proxy" },
    { edgeId: "classify-audio", sourceNodeId: "classify", targetNodeId: "audio" },
    { edgeId: "classify-subtitle", sourceNodeId: "classify", targetNodeId: "subtitle" },
    { edgeId: "classify-video", sourceNodeId: "classify", targetNodeId: "video" },
    { edgeId: "video-done", sourceNodeId: "video", targetNodeId: "done" }
  ]
};

const classifyNodeDetailsResponse = {
  workflowInstanceId: "workflow-local-001",
  nodeId: "classify",
  timeline: [
    {
      occurredAt: "2026-05-03T18:42:00Z",
      status: "Running",
      message: "Classification started",
      correlationId: "node-classify"
    }
  ],
  logs: [
    {
      occurredAt: "2026-05-03T18:42:03Z",
      level: "Information",
      message: "Command runner accepted classify work",
      correlationId: "node-classify",
      traceId: "trace-classify-001",
      spanId: "span-classify-001"
    }
  ]
};
