using ArchPulse.Core.Model;
using ArchPulse.Core.Serialization;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class CheckCommand : AsyncCommand<CheckSettings>
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        CheckSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = ScanPipeline.Scan(settings.Path);

            if (settings.Json)
            {
                Console.WriteLine(ArchPulseJson.Serialize(outcome));
            }
            else
            {
                TerminalRenderer.WriteCheck(outcome, Path.GetFullPath(settings.Path));
            }

            var threshold = CliParsing.ParseFailOn(settings.FailOn, ViolationSeverity.Error);
            return Task.FromResult(CliParsing.ExitCode(outcome, threshold));
        }
        catch (Exception exception)
        {
            return Task.FromResult(TerminalRenderer.Error(exception.Message));
        }
    }
}
