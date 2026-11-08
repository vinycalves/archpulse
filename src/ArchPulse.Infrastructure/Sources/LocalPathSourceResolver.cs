using ArchPulse.Infrastructure.Scanning;
using Microsoft.Extensions.Options;

namespace ArchPulse.Infrastructure.Sources;

public sealed class LocalPathSourceResolver(IOptions<ScannerOptions> options) : ISourceResolver
{
    public bool CanResolve(string sourceType) =>
        sourceType.Equals(ScanSourceTypes.Path, StringComparison.OrdinalIgnoreCase);

    public Task<SourceWorkspace> ResolveAsync(
        ScanSource source,
        CancellationToken cancellationToken)
    {
        var allowedRoots = options.Value.AllowedRoots.Length > 0
            ? options.Value.AllowedRoots
            : [Directory.GetCurrentDirectory()];

        var candidate = Path.IsPathRooted(source.Value)
            ? source.Value
            : Path.Combine(allowedRoots[0], source.Value);
        var fullPath = Path.GetFullPath(candidate);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Scan path '{fullPath}' does not exist.");
        }

        if (!allowedRoots.Any(root => IsUnder(fullPath, root)))
        {
            throw new UnauthorizedAccessException(
                $"Path '{fullPath}' is outside the configured scanner roots ({string.Join(", ", allowedRoots)}).");
        }

        return Task.FromResult(new SourceWorkspace(fullPath));
    }

    private static bool IsUnder(string path, string root)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var normalizedRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(normalizedRoot, comparison);
    }
}
