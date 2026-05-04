using MediaIngest.Contracts.Workflow;

namespace MediaIngest.Workflow;

public sealed class PackageWorkflowStarter
{
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
            PreparedChildWork: PackageWorkflowChildWorkPlan.FullLifecycle);
    }
}
