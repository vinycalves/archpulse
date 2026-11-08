namespace ArchPulse.Core.Scanning;

/// <summary>
/// Normalized metadata extracted from a single .NET project file.
/// </summary>
public sealed record ProjectInfo(
    string Name,
    string FullPath,
    string RelativePath,
    string Sdk,
    string? OutputType,
    bool IsTest,
    IReadOnlyList<string> ProjectReferencePaths,
    IReadOnlyList<string> PackageReferences,
    IReadOnlyDictionary<string, string> Properties);
