import { describe, expect, it } from "vitest";

import {
  buildMermaidFlowchart,
  mockedWorkflowGraph,
  workflowStatusPalette
} from "./workflowGraph";

describe("workflow graph mermaid syntax", () => {
  it("builds flowchart syntax with node classes and target-status link colors", () => {
    const diagram = buildMermaidFlowchart(mockedWorkflowGraph);

    expect(diagram).toContain("flowchart LR");
    expect(diagram).toContain('manifest["Manifest detected"]:::succeeded');
    expect(diagram).toContain('classify["Classify package"]:::running');
    expect(diagram).toContain('proxy["Proxy workflow"]:::waiting');
    expect(diagram).toContain("manifest --> scan");
    expect(diagram).toContain(
      `linkStyle 1 stroke:${workflowStatusPalette.Running.stroke}`
    );
    expect(diagram).toContain(
      `linkStyle 3 stroke:${workflowStatusPalette.Waiting.stroke}`
    );
  });

  it("escapes labels and sanitizes node identifiers for Mermaid", () => {
    const diagram = buildMermaidFlowchart({
      workflowInstanceId: "workflow-escape",
      workflowName: "Escaped workflow",
      packageId: "package-escape",
      nodes: [
        {
          nodeId: "scan.package/1",
          displayName: 'Scan "package" [ready]',
          kind: "Activity",
          status: "Succeeded",
          workflowInstanceId: "workflow-escape",
          packageId: "package-escape"
        },
        {
          nodeId: "classify.package/1",
          displayName: "Classify package",
          kind: "Activity",
          status: "Queued",
          workflowInstanceId: "workflow-escape",
          packageId: "package-escape"
        }
      ],
      edges: [
        {
          edgeId: "scan-classify",
          sourceNodeId: "scan.package/1",
          targetNodeId: "classify.package/1"
        }
      ]
    });

    expect(diagram).toContain('scan_package_1["Scan #quot;package#quot; #91;ready#93;"]:::succeeded');
    expect(diagram).toContain("scan_package_1 --> classify_package_1");
    expect(diagram).toContain(
      `linkStyle 0 stroke:${workflowStatusPalette.Queued.stroke}`
    );
  });
});
