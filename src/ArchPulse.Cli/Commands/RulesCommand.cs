using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class RulesCommand : Command<RulesSettings>
{
    public override int Execute(
        CommandContext context,
        RulesSettings settings,
        CancellationToken cancellationToken)
    {
        TerminalRenderer.WriteRules();
        return 0;
    }
}
