namespace MediaIngest.Worker.Watcher;

public interface IIngestPackageCandidateSink
{
    ValueTask ObserveAsync(IngestPackageCandidate candidate, CancellationToken cancellationToken);
}
