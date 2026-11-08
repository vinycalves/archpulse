using ArchPulse.Core.Rendering;
using ArchPulse.Core.Serialization;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class GraphCommand : AsyncCommand<GraphSettings>
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        GraphSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = ScanPipeline.Scan(settings.Path);

            switch (settings.Format)
            {
                case "json":
                    Console.WriteLine(ArchPulseJson.Serialize(outcome.Graph));
                    break;
                case "mermaid":
                    Console.WriteLine(MermaidRenderer.Render(outcome.Graph));
                    break;
                default:
                    TerminalRenderer.WriteGraph(outcome.Graph, settings.Depth, settings.All);
                    break;
            }

            return Task.FromResult(0);
        }
        catch (Exception exception)
        {
            return Task.FromResult(TerminalRenderer.Error(exception.Message));
        }
    }
}
