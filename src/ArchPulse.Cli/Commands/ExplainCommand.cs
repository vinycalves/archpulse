using ArchPulse.Core.Rules;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class ExplainCommand : Command<ExplainSettings>
{
    public override int Execute(
        CommandContext context,
        ExplainSettings settings,
        CancellationToken cancellationToken)
    {
        var rule = RuleCatalog.Find(settings.Rule);
        if (rule is null)
        {
            AnsiConsole.MarkupLine(
                $"[red]Unknown rule[/] [bold]{Markup.Escape(settings.Rule)}[/].");
            AnsiConsole.MarkupLine("[grey]Run `archpulse rules` to list the available rules.[/]");
            return 1;
        }

        TerminalRenderer.WriteRule(rule);
        return 0;
    }
}
