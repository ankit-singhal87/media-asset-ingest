namespace MediaIngest.Workflow;

internal sealed class PackageWorkflowLifecycle
{
    private readonly List<PackageWorkflowLifecycleSnapshot> events;
    private readonly PackageIngestRequest request;
    private readonly PackageWorkflowStarter workflowStarter;

    private PackageWorkflowLifecycle(
        PackageIngestRequest request,
        PackageWorkflowStarter workflowStarter,
        PackageWorkflowLifecycleSnapshot observed)
    {
        this.request = request;
        this.workflowStarter = workflowStarter;
        events = [observed];
        Current = observed;
    }

    public PackageWorkflowLifecycleSnapshot Current { get; private set; }

    public IReadOnlyList<PackageWorkflowLifecycleSnapshot> Events => events;

    public static PackageWorkflowLifecycle Observe(PackageIngestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequired(request.PackageId, "Package id is required.", nameof(request));
        ValidateRequired(request.PackagePath, "Package path is required.", nameof(request));
        ValidateRequired(request.CorrelationId, "Correlation id is required.", nameof(request));

        var observed = new PackageWorkflowLifecycleSnapshot(
            PackageId: request.PackageId,
            PackagePath: request.PackagePath,
            CorrelationId: request.CorrelationId,
            State: PackageWorkflowLifecycleState.Observed,
            OccurredAt: request.AcceptedAt);

        return new PackageWorkflowLifecycle(request, new PackageWorkflowStarter(), observed);
    }

    public PackageWorkflowLifecycleSnapshot MarkReady(DateTimeOffset occurredAt)
    {
        EnsureCurrentState(PackageWorkflowLifecycleState.Observed, PackageWorkflowLifecycleState.Ready);

        return TransitionTo(
            PackageWorkflowLifecycleState.Ready,
            occurredAt,
            workflowInstanceId: null,
            failureReason: null);
    }

    public PackageWorkflowStart Start(DateTimeOffset occurredAt)
    {
        EnsureCurrentState(PackageWorkflowLifecycleState.Ready, PackageWorkflowLifecycleState.Started);

        var start = workflowStarter.Start(request);
        TransitionTo(
            PackageWorkflowLifecycleState.Started,
            occurredAt,
            start.WorkflowInstanceId,
            failureReason: null);

        return start;
    }

    public PackageWorkflowLifecycleSnapshot MarkSucceeded(DateTimeOffset occurredAt)
    {
        EnsureCurrentState(PackageWorkflowLifecycleState.Started, PackageWorkflowLifecycleState.Succeeded);

        return TransitionTo(
            PackageWorkflowLifecycleState.Succeeded,
            occurredAt,
            Current.WorkflowInstanceId,
            failureReason: null);
    }

    public PackageWorkflowLifecycleSnapshot MarkFailed(string failureReason, DateTimeOffset occurredAt)
    {
        EnsureCurrentState(PackageWorkflowLifecycleState.Started, PackageWorkflowLifecycleState.Failed);
        ValidateRequired(failureReason, "Failure reason is required.", nameof(failureReason));

        return TransitionTo(
            PackageWorkflowLifecycleState.Failed,
            occurredAt,
            Current.WorkflowInstanceId,
            failureReason);
    }

    private PackageWorkflowLifecycleSnapshot TransitionTo(
        PackageWorkflowLifecycleState state,
        DateTimeOffset occurredAt,
        string? workflowInstanceId,
        string? failureReason)
    {
        var snapshot = new PackageWorkflowLifecycleSnapshot(
            PackageId: Current.PackageId,
            PackagePath: Current.PackagePath,
            CorrelationId: Current.CorrelationId,
            State: state,
            OccurredAt: occurredAt,
            WorkflowInstanceId: workflowInstanceId,
            FailureReason: failureReason);

        events.Add(snapshot);
        Current = snapshot;

        return snapshot;
    }

    private void EnsureCurrentState(
        PackageWorkflowLifecycleState expected,
        PackageWorkflowLifecycleState requested)
    {
        if (Current.State != expected)
        {
            throw new InvalidOperationException(
                $"Cannot move package '{Current.PackageId}' from '{Current.State}' to '{requested}'.");
        }
    }

    private static void ValidateRequired(string value, string message, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }
    }
}
