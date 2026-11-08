namespace ArchPulse.Infrastructure.Sources;

public interface ISourceResolver
{
    bool CanResolve(string sourceType);

    Task<SourceWorkspace> ResolveAsync(
        ScanSource source,
        CancellationToken cancellationToken);
}
