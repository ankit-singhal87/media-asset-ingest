import { describe, expect, it } from "vitest";

import {
  buildMermaidFlowchart,
  mockedWorkflowGraph,
  toMermaidNodeId,
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

  it("renders orchestrator waits, command dependency nodes, and finalization", () => {
    const diagram = buildMermaidFlowchart({
      workflowInstanceId: "package-asset-001",
      workflowName: "PackageIngestWorkflow",
      packageId: "asset-001",
      nodes: [
        {
          nodeId: "dispatch-processing",
          displayName: "Dispatch processing work",
          kind: "CommandDispatch",
          status: "Succeeded",
          workflowInstanceId: "package-asset-001",
          packageId: "asset-001"
        },
        {
          nodeId: "command-media-source-mov",
          displayName: "CreateProxy source.mov",
          kind: "WorkItem",
          status: "Succeeded",
          workflowInstanceId: "package-asset-001",
          packageId: "asset-001",
          workItemId: "command-asset-001-media-source-mov"
        },
        {
          nodeId: "wait-command-completion",
          displayName: "Wait for command completion",
          kind: "Wait",
          status: "Waiting",
          workflowInstanceId: "package-asset-001",
          packageId: "asset-001"
        },
        {
          nodeId: "complete-processing",
          displayName: "Complete processing commands",
          kind: "CommandCompletion",
          status: "Queued",
          workflowInstanceId: "package-asset-001",
          packageId: "asset-001"
        },
        {
          nodeId: "finalize-package",
          displayName: "Finalize package",
          kind: "Finalization",
          status: "Pending",
          workflowInstanceId: "package-asset-001",
          packageId: "asset-001"
        }
      ],
      edges: [
        {
          edgeId: "dispatch-command",
          sourceNodeId: "dispatch-processing",
          targetNodeId: "command-media-source-mov"
        },
        {
          edgeId: "command-wait",
          sourceNodeId: "command-media-source-mov",
          targetNodeId: "wait-command-completion"
        },
        {
          edgeId: "wait-complete",
          sourceNodeId: "wait-command-completion",
          targetNodeId: "complete-processing"
        },
        {
          edgeId: "complete-finalize",
          sourceNodeId: "complete-processing",
          targetNodeId: "finalize-package"
        }
      ]
    });

    expect(diagram).toContain('dispatch_processing["Dispatch processing work"]:::succeeded');
    expect(diagram).toContain('wait_command_completion["Wait for command completion"]:::waiting');
    expect(diagram).toContain('complete_processing["Complete processing commands"]:::queued');
    expect(diagram).toContain('finalize_package["Finalize package"]:::pending');
    expect(diagram).toContain("dispatch_processing --> command_media_source_mov");
    expect(diagram).toContain("command_media_source_mov --> wait_command_completion");
    expect(diagram).toContain("wait_command_completion --> complete_processing");
    expect(diagram).toContain("complete_processing --> finalize_package");
  });

  it("exposes the stable Mermaid node identifier used for SVG interaction", () => {
    expect(toMermaidNodeId("scan.package/1")).toBe("scan_package_1");
    expect(toMermaidNodeId("2026-start")).toBe("node_2026_start");
  });
});
