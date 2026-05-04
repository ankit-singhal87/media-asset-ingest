using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow;

public sealed class PackageWorkflowStarter
{
    private static readonly PreparedChildWork[] PackageStartPlan =
    [
        new("scan-package", "Package scan"),
        new("classify-files", "Classify discovered files"),
        new("dispatch-processing", "Dispatch processing work"),
        new("reconcile-package", "Reconcile package"),
        new("finalize-package", "Finalize package")
    ];

    public PackageWorkflowStart Start(PackageIngestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.PackageId))
        {
            throw new ArgumentException("Package id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PackagePath))
        {
            throw new ArgumentException("Package path is required.", nameof(request));
        }

        return new PackageWorkflowStart(
            PackageId: request.PackageId,
            PackagePath: request.PackagePath,
            WorkflowName: WorkflowContractNames.PackageIngestWorkflow,
            WorkflowInstanceId: $"package-{request.PackageId}",
            CorrelationId: request.CorrelationId,
            AcceptedAt: request.AcceptedAt,
            PreparedChildWork: PackageStartPlan);
    }
}
