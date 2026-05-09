using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow.Orchestrator;

[WorkflowDefinition(WorkflowContractNames.PackageIngestWorkflow, "Package ingest workflow")]
[WorkflowNode("package-start", "Package ingest", WorkflowNodeKind.WorkflowStep)]
[WorkflowNode("scan-package", "Package scan", WorkflowNodeKind.ChildWorkflow, WorkflowContractNames.PackageScanWorkflow)]
[WorkflowNode("classify-files", "Classify discovered files", WorkflowNodeKind.ChildWorkflow, WorkflowContractNames.FileClassificationWorkflow)]
[WorkflowNode("essence-group-processing", "Essence group processing", WorkflowNodeKind.ChildWorkflow, WorkflowContractNames.EssenceGroupProcessingWorkflow)]
[WorkflowNode("proxy-creation", "Proxy creation", WorkflowNodeKind.ChildWorkflow, WorkflowContractNames.ProxyCreationWorkflow)]
[WorkflowNode("dispatch-processing", "Dispatch processing work", WorkflowNodeKind.CommandDispatch)]
[WorkflowNode("command-work", "Command work", WorkflowNodeKind.WorkItem)]
[WorkflowNode("wait-command-completion", "Wait for command completion", WorkflowNodeKind.Wait)]
[WorkflowNode("complete-processing", "Complete processing commands", WorkflowNodeKind.CommandCompletion)]
[WorkflowNode("reconcile-package", "Reconcile package", WorkflowNodeKind.ChildWorkflow, WorkflowContractNames.ReconciliationWorkflow)]
[WorkflowNode("wait-done-marker", "Wait for done marker", WorkflowNodeKind.Wait)]
[WorkflowNode("finalize-package", "Finalize package", WorkflowNodeKind.Finalization, WorkflowContractNames.FinalizationWorkflow)]
[WorkflowEdge("package-start", "scan-package")]
[WorkflowEdge("scan-package", "classify-files")]
[WorkflowEdge("classify-files", "essence-group-processing")]
[WorkflowEdge("essence-group-processing", "proxy-creation")]
[WorkflowEdge("proxy-creation", "dispatch-processing")]
[WorkflowEdge("dispatch-processing", "command-work")]
[WorkflowEdge("command-work", "wait-command-completion")]
[WorkflowEdge("wait-command-completion", "complete-processing")]
[WorkflowEdge("complete-processing", "reconcile-package")]
[WorkflowEdge("reconcile-package", "wait-done-marker")]
[WorkflowEdge("wait-done-marker", "finalize-package")]
public sealed class PackageIngestWorkflowDefinition;
