using ArchPulse.Core.Analysis;
using ArchPulse.Core.Model;
using ArchPulse.Infrastructure.Sources;
using Microsoft.Extensions.Logging;

namespace ArchPulse.Infrastructure.Scanning;

public sealed class ScanRunner(
    ISourceResolver sourceResolver,
    RepositoryScanner scanner,
    ILogger<ScanRunner> logger)
{
    public async Task<ScanOutcome> RunAsync(
        ScanSource source,
        CancellationToken cancellationToken)
    {
        await using var workspace = await sourceResolver.ResolveAsync(source, cancellationToken);

        logger.LogInformation(
            "Scanning {SourceType} source at {RootPath}.",
            source.Type,
            workspace.RootPath);

        return scanner.Scan(workspace.RootPath);
    }
}
