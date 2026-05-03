import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { App } from "./App";

describe("workflow graph control plane", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("displays mocked workflow nodes and enough state to inspect progress", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ packages: [] }), {
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

    const graph = screen.getByRole("list", { name: /mocked workflow graph/i });
    const nodes = within(graph).getAllByRole("listitem");

    expect(nodes).toHaveLength(7);
    expect(
      within(graph).getByRole("listitem", { name: /manifest detected succeeded/i })
    ).toBeInTheDocument();
    expect(
      within(graph).getByRole("listitem", { name: /classify package running/i })
    ).toBeInTheDocument();
    expect(
      within(graph).getByRole("listitem", { name: /proxy workflow waiting/i })
    ).toBeInTheDocument();
    expect(
      within(graph).getByRole("listitem", { name: /audio essence pending/i })
    ).toBeInTheDocument();

    expect(screen.getByText(/pending: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/running: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/failed: 1/i)).toBeInTheDocument();
    expect(screen.getByText(/waiting: 1/i)).toBeInTheDocument();
    expect(await screen.findByText(/no packages reported yet/i)).toBeInTheDocument();
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
      .mockResolvedValueOnce(new Response(null, { status: 202 }));

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
  });

  it("shows real package status from the ingest status endpoint separately from the mocked graph", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(
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
    expect(
      screen.getByRole("list", { name: /mocked workflow graph/i })
    ).toBeInTheDocument();
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
