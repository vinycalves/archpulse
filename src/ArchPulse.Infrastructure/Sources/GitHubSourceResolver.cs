using System.Diagnostics;
using ArchPulse.Infrastructure.Scanning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchPulse.Infrastructure.Sources;

public sealed class GitHubSourceResolver(
    IOptions<ScannerOptions> options,
    ILogger<GitHubSourceResolver> logger) : ISourceResolver
{
    public bool CanResolve(string sourceType) =>
        sourceType.Equals(ScanSourceTypes.GitHub, StringComparison.OrdinalIgnoreCase);

    public async Task<SourceWorkspace> ResolveAsync(
        ScanSource source,
        CancellationToken cancellationToken)
    {
        var repository = ParseRepository(source.Value);
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "archpulse",
            Guid.NewGuid().ToString("N"));
        var clonePath = Path.Combine(tempRoot, "repo");

        Directory.CreateDirectory(tempRoot);
        logger.LogInformation("Cloning public repository {Repository}.", repository);

        try
        {
            var startInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = tempRoot,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("clone");
            startInfo.ArgumentList.Add("--depth");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("--single-branch");
            startInfo.ArgumentList.Add("--no-tags");
            startInfo.ArgumentList.Add($"https://github.com/{repository}.git");
            startInfo.ArgumentList.Add(clonePath);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start git.");

            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(
                options.Value.GitTimeoutSeconds > 0 ? options.Value.GitTimeoutSeconds : 90));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process may have exited between the timeout and the kill.
                }

                throw new TimeoutException(
                    $"git clone for '{repository}' exceeded the configured timeout.");
            }

            var stderr = await stderrTask;
            _ = await stdoutTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"git clone failed for '{repository}': {stderr.Trim()}");
            }

            return new SourceWorkspace(clonePath, tempRoot);
        }
        catch
        {
            TryDelete(tempRoot);
            throw;
        }
    }

    private static string ParseRepository(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only public HTTPS GitHub repositories are supported (https://github.com/owner/repo).");
        }

        var segments = uri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length != 2)
        {
            throw new ArgumentException(
                "Repository URL must have the form https://github.com/owner/repo.");
        }

        var repository = segments[1];
        if (repository.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repository = repository[..^4];
        }

        return $"{segments[0]}/{repository}";
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup.
        }
    }
}
