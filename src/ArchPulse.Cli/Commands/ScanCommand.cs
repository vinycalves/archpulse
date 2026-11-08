using ArchPulse.Core.Rendering;
using ArchPulse.Core.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class ScanCommand : AsyncCommand<ScanSettings>
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        ScanSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = ScanPipeline.Scan(settings.Path);

            switch (settings.Format)
            {
                case "json":
                    Console.WriteLine(ArchPulseJson.Serialize(outcome));
                    break;
                case "markdown":
                    Console.WriteLine(MarkdownReportRenderer.Render(
                        outcome,
                        settings.Path,
                        DateTimeOffset.UtcNow));
                    break;
                case "mermaid":
                    Console.WriteLine(MermaidRenderer.Render(outcome.Graph));
                    break;
                default:
                    TerminalRenderer.WriteScan(outcome, settings.Path, settings.Details);
                    break;
            }

            if (settings.Output is not null)
            {
                var files = ScanPipeline.WriteArtifacts(outcome, settings.Path, settings.Output);
                TerminalRenderer.ErrorConsole.MarkupLine(
                    $"[grey]Wrote {files.Length} artifacts to {Markup.Escape(Path.GetFullPath(settings.Output))}[/]");
            }

            return Task.FromResult(
                CliParsing.ExitCode(outcome, CliParsing.ParseFailOn(settings.FailOn)));
        }
        catch (Exception exception)
        {
            return Task.FromResult(TerminalRenderer.Error(exception.Message));
        }
    }
}
