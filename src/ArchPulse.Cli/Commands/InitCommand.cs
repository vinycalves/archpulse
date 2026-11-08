using Spectre.Console;
using Spectre.Console.Cli;

namespace ArchPulse.Cli.Commands;

public sealed class InitCommand : Command<InitSettings>
{
    private const string StarterRules =
        """
        {
          "forbiddenDependencies": [
            {
              "id": "APP-001",
              "from": "*.Domain",
              "to": "*.Infrastructure",
              "severity": "Error",
              "title": "Domain must not depend on infrastructure",
              "message": "Keep domain projects free of persistence, messaging, and delivery concerns."
            }
          ]
        }
        """;

    public override int Execute(
        CommandContext context,
        InitSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            var root = Path.GetFullPath(settings.Path);
            var directory = Path.Combine(root, ".archpulse");
            var file = Path.Combine(directory, "rules.json");

            if (File.Exists(file) && !settings.Force)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]![/] [grey]{Markup.Escape(file)}[/] already exists. Use [bold]--force[/] to overwrite it.");
                return 1;
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(file, StarterRules);
            TerminalRenderer.WriteInit(file);
            return 0;
        }
        catch (Exception exception)
        {
            return TerminalRenderer.Error(exception.Message);
        }
    }
}
