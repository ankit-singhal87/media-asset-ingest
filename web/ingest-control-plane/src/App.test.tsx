import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { App } from "./App";

describe("workflow graph control plane", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("displays mocked workflow nodes and enough state to inspect progress", () => {
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
  });

  it("starts local ingest watching when the operator clicks Start ingest", async () => {
    const fetchMock = vi
      .spyOn(globalThis, "fetch")
      .mockResolvedValue(new Response(null, { status: 202 }));

    render(<App />);

    expect(screen.getByText(/local watcher: idle/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /start ingest/i }));

    expect(screen.getByText(/local watcher: starting/i)).toBeInTheDocument();

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith("/api/ingest/start", {
        method: "POST"
      });
    });
    expect(await screen.findByText(/local watcher: watching/i)).toBeInTheDocument();
  });

  it("shows an error status when local ingest start fails", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(null, { status: 500 })
    );

    render(<App />);

    fireEvent.click(screen.getByRole("button", { name: /start ingest/i }));

    expect(await screen.findByText(/local watcher: error/i)).toBeInTheDocument();
  });
});
