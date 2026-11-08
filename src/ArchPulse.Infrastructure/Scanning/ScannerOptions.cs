namespace ArchPulse.Infrastructure.Scanning;

public sealed class ScannerOptions
{
    /// <summary>
    /// Local paths the API and worker are allowed to scan. When empty, only the process
    /// working directory is allowed. Repository URLs are always cloned into a temp folder.
    /// </summary>
    public string[] AllowedRoots { get; set; } = [];

    public int GitTimeoutSeconds { get; set; } = 90;
}
