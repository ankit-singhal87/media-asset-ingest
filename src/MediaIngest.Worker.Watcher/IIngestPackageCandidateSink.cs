namespace MediaIngest.Worker.Watcher;

internal interface IIngestPackageCandidateSink
{
    ValueTask ObserveAsync(IngestPackageCandidate candidate, CancellationToken cancellationToken);
}
