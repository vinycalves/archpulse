namespace ArchPulse.Infrastructure.Sources;

/// <summary>
/// A resolved source tree. Temporary workspaces are removed on disposal.
/// </summary>
public sealed class SourceWorkspace : IAsyncDisposable
{
    private readonly string? _temporaryDirectory;

    public SourceWorkspace(string rootPath, string? temporaryDirectory = null)
    {
        RootPath = rootPath;
        _temporaryDirectory = temporaryDirectory;
    }

    public string RootPath { get; }

    public bool IsTemporary => _temporaryDirectory is not null;

    public ValueTask DisposeAsync()
    {
        if (_temporaryDirectory is not null && Directory.Exists(_temporaryDirectory))
        {
            try
            {
                Directory.Delete(_temporaryDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup: a failed temp cleanup must not fail the scan.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort cleanup.
            }
        }

        return ValueTask.CompletedTask;
    }
}
