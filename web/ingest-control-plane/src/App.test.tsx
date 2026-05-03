import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { App } from "./App";

describe("workflow graph control plane", () => {
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
});
