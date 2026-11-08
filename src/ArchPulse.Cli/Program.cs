using ArchPulse.Cli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("archpulse");
    config.SetApplicationVersion("0.2.0");

    config.AddCommand<ScanCommand>("scan")
        .WithDescription("Scan a C# repository and print an architecture report.");

    config.AddCommand<CheckCommand>("check")
        .WithDescription("Run architecture rules and return a CI-friendly exit code.");

    config.AddCommand<GraphCommand>("graph")
        .WithDescription("Print the project dependency graph as a terminal tree.");

    config.AddCommand<DiffCommand>("diff")
        .WithDescription("Compare two architecture scans.");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Create a starter .archpulse/rules.json.");

    config.AddCommand<RulesCommand>("rules")
        .WithDescription("List the built-in architecture rules.");

    config.AddCommand<ExplainCommand>("explain")
        .WithDescription("Explain an architecture rule.");

    config.AddCommand<WatchCommand>("watch")
        .WithDescription("Watch a repository and re-run the check on changes.");
});

return await app.RunAsync(args);
