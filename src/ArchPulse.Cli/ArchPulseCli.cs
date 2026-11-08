using System.Text.Json;
using ArchPulse.Core.Analysis;
using ArchPulse.Core.Diff;
using ArchPulse.Core.Model;
using ArchPulse.Core.Rendering;
using ArchPulse.Core.Serialization;

namespace ArchPulse.Cli;

public static class ArchPulseCli
{
    private const string Version = "0.1.0";

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "scan" => await ScanAsync(args[1..]),
                "diff" => await DiffAsync(args[1..]),
                "version" or "--version" => PrintVersion(),
                _ => UnknownCommand(args[0])
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 2;
        }
    }

    private static Task<int> ScanAsync(string[] args)
    {
        string? path = null;
        var output = Path.Combine("artifacts", "archpulse");
        var format = "all";
        ViolationSeverity? failOn = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--output" or "-o":
                    output = RequireValue(args, ref index, "--output");
                    break;
                case "--format" or "-f":
                    format = RequireValue(args, ref index, "--format").ToLowerInvariant();
                    break;
                case "--fail-on":
                    failOn = ParseSeverity(RequireValue(args, ref index, "--fail-on"));
                    break;
                case "--help" or "-h":
                    PrintScanHelp();
                    return Task.FromResult(0);
                default:
                    if (args[index].StartsWith('-'))
                    {
                        throw new ArgumentException($"Unknown option '{args[index]}'.");
                    }

                    if (path is not null)
                    {
                        throw new ArgumentException("scan accepts exactly one path.");
                    }

                    path = args[index];
                    break;
            }
        }

        if (path is null)
        {
            throw new ArgumentException("scan requires a repository path.");
        }

        if (format is not ("all" or "json" or "markdown" or "mermaid"))
        {
            throw new ArgumentException("--format must be all, json, markdown, or mermaid.");
        }

        var outcome = new RepositoryScanner().Scan(path);
        var outputDirectory = Path.GetFullPath(output);
        Directory.CreateDirectory(outputDirectory);

        if (format is "all" or "json")
        {
            File.WriteAllText(
                Path.Combine(outputDirectory, "architecture.json"),
                ArchPulseJson.Serialize(outcome));
        }

        if (format is "all" or "markdown")
        {
            File.WriteAllText(
                Path.Combine(outputDirectory, "report.md"),
                MarkdownReportRenderer.Render(outcome, path, DateTimeOffset.UtcNow));
        }

        if (format is "all" or "mermaid")
        {
            File.WriteAllText(
                Path.Combine(outputDirectory, "architecture.mmd"),
                MermaidRenderer.Render(outcome.Graph));
        }

        Console.WriteLine($"ArchPulse scan: {Path.GetFullPath(path)}");
        Console.WriteLine(
            $"Components: {outcome.Summary.Components} | "
            + $"Dependencies: {outcome.Summary.Dependencies} | "
            + $"Violations: {outcome.Summary.Violations} "
            + $"(errors: {outcome.Summary.Errors}, warnings: {outcome.Summary.Warnings})");
        Console.WriteLine($"Output: {outputDirectory}");

        foreach (var violation in outcome.Violations.Take(20))
        {
            Console.WriteLine(
                $"  [{violation.Severity}] {violation.RuleId}: {violation.Message}");
        }

        if (outcome.Violations.Count > 20)
        {
            Console.WriteLine($"  ... and {outcome.Violations.Count - 20} more violations.");
        }

        if (failOn is not null
            && outcome.Violations.Any(violation => violation.Severity >= failOn))
        {
            Console.Error.WriteLine($"ArchPulse failed the build: violations at or above {failOn} were found.");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    private static Task<int> DiffAsync(string[] args)
    {
        string? beforePath = null;
        string? afterPath = null;
        string? output = null;
        var format = "markdown";
        var failOnNew = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--output" or "-o":
                    output = RequireValue(args, ref index, "--output");
                    break;
                case "--format" or "-f":
                    format = RequireValue(args, ref index, "--format").ToLowerInvariant();
                    break;
                case "--fail-on-new":
                    failOnNew = true;
                    break;
                case "--help" or "-h":
                    PrintDiffHelp();
                    return Task.FromResult(0);
                default:
                    if (args[index].StartsWith('-'))
                    {
                        throw new ArgumentException($"Unknown option '{args[index]}'.");
                    }

                    if (beforePath is null)
                    {
                        beforePath = args[index];
                    }
                    else if (afterPath is null)
                    {
                        afterPath = args[index];
                    }
                    else
                    {
                        throw new ArgumentException("diff accepts exactly two scan JSON files.");
                    }

                    break;
            }
        }

        if (beforePath is null || afterPath is null)
        {
            throw new ArgumentException("diff requires a baseline and a current scan JSON file.");
        }

        if (format is not ("markdown" or "json"))
        {
            throw new ArgumentException("--format must be markdown or json.");
        }

        var before = LoadOutcome(beforePath);
        var after = LoadOutcome(afterPath);
        var diff = ArchitectureDiffer.Compare(before, after);

        var rendered = format == "json"
            ? ArchPulseJson.Serialize(diff)
            : MarkdownDiffRenderer.Render(diff, beforePath, afterPath);

        if (output is not null)
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(output));
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(output, rendered);
            Console.WriteLine($"Architecture diff written to {Path.GetFullPath(output)}");
        }
        else
        {
            Console.Out.Write(rendered);
        }

        Console.Error.WriteLine(
            $"Architecture diff: +{diff.AddedComponents.Count}/-{diff.RemovedComponents.Count} components, "
            + $"+{diff.AddedDependencies.Count}/-{diff.RemovedDependencies.Count} dependencies, "
            + $"{diff.NewViolations.Count} new / {diff.ResolvedViolations.Count} resolved violations.");

        if (failOnNew
            && diff.NewViolations.Any(violation =>
                violation.Severity is ViolationSeverity.Error or ViolationSeverity.Critical))
        {
            Console.Error.WriteLine("ArchPulse failed the build: new architecture errors were introduced.");
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }

    private static ScanOutcome LoadOutcome(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Scan file '{path}' was not found.", path);
        }

        var json = File.ReadAllText(path);

        try
        {
            var outcome = ArchPulseJson.Deserialize<ScanOutcome>(json);
            if (outcome?.Graph is not null)
            {
                return outcome;
            }
        }
        catch (JsonException)
        {
            // Fall back to a raw ArchitectureGraph document.
        }

        var graph = ArchPulseJson.Deserialize<ArchitectureGraph>(json)
            ?? throw new InvalidOperationException($"'{path}' is not a valid ArchPulse scan or graph.");

        var summary = ArchitectureSummary.From(graph, []);
        return new ScanOutcome(graph, [], summary);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        return args[index];
    }

    private static ViolationSeverity? ParseSeverity(string value) =>
        value.ToLowerInvariant() switch
        {
            "none" => null,
            "info" => ViolationSeverity.Info,
            "warning" or "warn" => ViolationSeverity.Warning,
            "error" => ViolationSeverity.Error,
            "critical" => ViolationSeverity.Critical,
            _ => throw new ArgumentException(
                "--fail-on must be none, info, warning, error, or critical.")
        };

    private static bool IsHelp(string value) =>
        value is "help" or "--help" or "-h" or "-?";

    private static int PrintVersion()
    {
        Console.WriteLine($"ArchPulse {Version}");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"error: unknown command '{command}'. Run 'archpulse --help'.");
        return 2;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            ArchPulse — living architecture for .NET and Aspire applications.

            Usage:
              archpulse scan <path> [options]
              archpulse diff <before.json> <after.json> [options]
              archpulse version
              archpulse --help

            Commands:
              scan     Discover projects, Aspire resources, dependencies, and violations.
              diff     Compare two scans and render an architecture change report.
              version  Print the ArchPulse version.

            Run 'archpulse <command> --help' for command-specific options.
            """);
    }

    private static void PrintScanHelp()
    {
        Console.WriteLine(
            """
            Usage: archpulse scan <path> [options]

            Options:
              -o, --output <dir>    Output directory (default: artifacts/archpulse).
              -f, --format <value>  all, json, markdown, or mermaid (default: all).
                  --fail-on <level> Exit with code 1 for violations at or above
                                    info, warning, error, or critical.
              -h, --help            Show this help.

            Examples:
              archpulse scan .
              archpulse scan samples/ContosoShop --print
              archpulse scan . --fail-on error
            """);
    }

    private static void PrintDiffHelp()
    {
        Console.WriteLine(
            """
            Usage: archpulse diff <before.json> <after.json> [options]

            Options:
              -o, --output <file>   Write the report to a file instead of stdout.
              -f, --format <value>  markdown or json (default: markdown).
                  --fail-on-new     Exit with code 1 when new errors are introduced.
              -h, --help            Show this help.

            Examples:
              archpulse diff baseline/architecture.json current/architecture.json
              archpulse diff before.json after.json --format json --output diff.json
            """);
    }
}
