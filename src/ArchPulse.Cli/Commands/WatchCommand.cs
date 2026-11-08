using ArchPulse.Core.Model;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class WatchCommand : AsyncCommand<WatchSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        WatchSettings settings,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(settings.Path);
        if (!Directory.Exists(root))
        {
            return TerminalRenderer.Error($"Directory '{root}' does not exist.");
        }

        var threshold = CliParsing.ParseFailOn(settings.FailOn, ViolationSeverity.Error);

        using var watcher = new FileSystemWatcher(root)
        {
            IncludeSubdirectories = true,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.LastWrite
                | NotifyFilters.FileName
                | NotifyFilters.DirectoryName
        };

        using var pending = new ManualResetEventSlim(false);
        using var cancellation = new CancellationTokenSource();

        void OnChanged(object sender, FileSystemEventArgs args)
        {
            if (IsRelevant(args.FullPath))
            {
                pending.Set();
            }
        }

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += (_, args) =>
        {
            if (IsRelevant(args.FullPath))
            {
                pending.Set();
            }
        };

        watcher.EnableRaisingEvents = true;

        ConsoleCancelEventHandler cancelHandler = (_, args) =>
        {
            args.Cancel = true;
            cancellation.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;
        AnsiConsole.MarkupLine(
            $"[grey]Watching {Markup.Escape(root)}. Press Ctrl+C to stop.[/]");

        try
        {
            RunCheck(root, threshold);

            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    if (!pending.Wait(500, cancellation.Token))
                    {
                        continue;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                pending.Reset();

                try
                {
                    await Task.Delay(250, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                RunCheck(root, threshold);
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }

        AnsiConsole.MarkupLine("[grey]Watch stopped.[/]");
        return 0;
    }

    private static void RunCheck(string root, ViolationSeverity? threshold)
    {
        try
        {
            var outcome = ScanPipeline.Scan(root);

            if (!Console.IsOutputRedirected)
            {
                AnsiConsole.Clear();
            }

            AnsiConsole.MarkupLine($"[grey]{DateTimeOffset.Now:HH:mm:ss}[/]");
            TerminalRenderer.WriteCheck(outcome, root);

            if (threshold is not null
                && outcome.Violations.Any(violation => violation.Severity >= threshold))
            {
                AnsiConsole.MarkupLine(
                    "[red]The configured threshold would fail this check.[/]");
            }
        }
        catch (Exception exception)
        {
            TerminalRenderer.ErrorConsole.MarkupLine($"[red]error:[/] {Markup.Escape(exception.Message)}");
        }
    }

    private static bool IsRelevant(string path)
    {
        if (path.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase)
            || path.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase)
            || path.Contains(
                $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(path);
        return extension is ".cs" or ".csproj" or ".sln" or ".slnx" or ".props" or ".targets"
            || (path.Contains(".archpulse", StringComparison.OrdinalIgnoreCase)
                && extension == ".json");
    }
}
