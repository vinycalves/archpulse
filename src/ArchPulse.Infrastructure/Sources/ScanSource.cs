namespace ArchPulse.Infrastructure.Sources;

public sealed record ScanSource(string Type, string Value);

public static class ScanSourceTypes
{
    public const string Path = "path";
    public const string GitHub = "github";

    public static readonly string[] All = [Path, GitHub];
}
