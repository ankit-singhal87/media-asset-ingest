namespace MediaIngest.Worker.Watcher;

public sealed class CallbackIngestPackageCandidateSink(
    Func<IngestPackageCandidate, CancellationToken, ValueTask> observe)
    : IIngestPackageCandidateSink
{
    private readonly Func<IngestPackageCandidate, CancellationToken, ValueTask> observe =
        observe ?? throw new ArgumentNullException(nameof(observe));

    public ValueTask ObserveAsync(IngestPackageCandidate candidate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return observe(candidate, cancellationToken);
    }
}
