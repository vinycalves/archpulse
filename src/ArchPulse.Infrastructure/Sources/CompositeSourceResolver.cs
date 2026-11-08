namespace ArchPulse.Infrastructure.Sources;

public sealed class CompositeSourceResolver(
    LocalPathSourceResolver localPath,
    GitHubSourceResolver gitHub) : ISourceResolver
{
    public bool CanResolve(string sourceType) =>
        ScanSourceTypes.All.Contains(sourceType, StringComparer.OrdinalIgnoreCase);

    public Task<SourceWorkspace> ResolveAsync(
        ScanSource source,
        CancellationToken cancellationToken) =>
        source.Type.ToLowerInvariant() switch
        {
            ScanSourceTypes.Path => localPath.ResolveAsync(source, cancellationToken),
            ScanSourceTypes.GitHub => gitHub.ResolveAsync(source, cancellationToken),
            _ => throw new NotSupportedException(
                $"Source type '{source.Type}' is not supported. Use 'path' or 'github'.")
        };
}
