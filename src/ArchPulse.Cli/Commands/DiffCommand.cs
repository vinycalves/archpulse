using ArchPulse.Core.Diff;
using ArchPulse.Core.Model;
using ArchPulse.Core.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class DiffCommand : AsyncCommand<DiffSettings>
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        DiffSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var before = ScanPipeline.LoadOutcome(settings.Before);
            var after = ScanPipeline.LoadOutcome(settings.After);
            var diff = ArchitectureDiffer.Compare(before, after);

            string? rendered = settings.Format switch
            {
                "json" => ArchPulseJson.Serialize(diff),
                "markdown" => MarkdownDiffRenderer.Render(diff, settings.Before, settings.After),
                _ => null
            };

            if (settings.Output is not null)
            {
                var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(settings.Output));
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var content = rendered
                    ?? MarkdownDiffRenderer.Render(diff, settings.Before, settings.After);
                File.WriteAllText(settings.Output, content);

                TerminalRenderer.ErrorConsole.MarkupLine(
                    $"[grey]Wrote architecture diff to {Markup.Escape(Path.GetFullPath(settings.Output))}[/]");
            }
            else if (rendered is not null)
            {
                Console.WriteLine(rendered);
            }

            if (settings.Format == "console")
            {
                TerminalRenderer.WriteDiff(
                    diff,
                    before,
                    after,
                    settings.Before,
                    settings.After);
            }

            if (settings.FailOnNew
                && diff.NewViolations.Any(violation =>
                    violation.Severity is ViolationSeverity.Error or ViolationSeverity.Critical))
            {
                TerminalRenderer.ErrorConsole.MarkupLine(
                    "[red]error:[/] the diff introduces new architecture errors.");
                return Task.FromResult(1);
            }

            return Task.FromResult(0);
        }
        catch (Exception exception)
        {
            return Task.FromResult(TerminalRenderer.Error(exception.Message));
        }
    }
}
