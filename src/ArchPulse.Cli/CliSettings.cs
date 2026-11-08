using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ArchPulse.Cli;

public class RepositorySettings : CommandSettings
{
    [CommandArgument(0, "[path]")]
    [Description("Repository, solution, or project path. Defaults to the current directory.")]
    public string Path { get; init; } = ".";

    public override ValidationResult Validate()
    {
        if (!Directory.Exists(Path) && !File.Exists(Path))
        {
            return ValidationResult.Error($"Path '{Path}' does not exist.");
        }

        return ValidationResult.Success();
    }
}

public sealed class ScanSettings : RepositorySettings
{
    [CommandOption("-f|--format <FORMAT>")]
    [Description("Output format: console, json, markdown, or mermaid.")]
    public string Format { get; init; } = "console";

    [CommandOption("-o|--output <DIR>")]
    [Description("Write architecture.json, architecture.mmd, and report.md to a directory.")]
    public string? Output { get; init; }

    [CommandOption("--fail-on <LEVEL>")]
    [Description("Exit with code 1 for violations at or above info, warning, error, or critical.")]
    public string? FailOn { get; init; }

    [CommandOption("--details")]
    [Description("Show project metadata such as target framework and SDK.")]
    public bool Details { get; init; }

    public override ValidationResult Validate()
    {
        var result = base.Validate();
        if (!result.Successful)
        {
            return result;
        }

        if (Format is not ("console" or "json" or "markdown" or "mermaid"))
        {
            return ValidationResult.Error("--format must be console, json, markdown, or mermaid.");
        }

        if (FailOn is not null && !CliParsing.IsSeverity(FailOn))
        {
            return ValidationResult.Error("--fail-on must be none, info, warning, error, or critical.");
        }

        return ValidationResult.Success();
    }
}

public sealed class CheckSettings : RepositorySettings
{
    [CommandOption("--fail-on <LEVEL>")]
    [Description("Exit with code 1 for violations at or above info, warning, error, or critical.")]
    public string FailOn { get; init; } = "error";

    [CommandOption("--json")]
    [Description("Write the scan result as JSON instead of a terminal report.")]
    public bool Json { get; init; }

    public override ValidationResult Validate()
    {
        var result = base.Validate();
        if (!result.Successful)
        {
            return result;
        }

        return CliParsing.IsSeverity(FailOn)
            ? ValidationResult.Success()
            : ValidationResult.Error("--fail-on must be none, info, warning, error, or critical.");
    }
}

public sealed class GraphSettings : RepositorySettings
{
    [CommandOption("-f|--format <FORMAT>")]
    [Description("Output format: tree, mermaid, or json.")]
    public string Format { get; init; } = "tree";

    [CommandOption("--depth <N>")]
    [Description("Maximum traversal depth. Zero means unlimited.")]
    public int Depth { get; init; }

    [CommandOption("--all")]
    [Description("Include external packages and Aspire resources in the tree.")]
    public bool All { get; init; }

    public override ValidationResult Validate()
    {
        var result = base.Validate();
        if (!result.Successful)
        {
            return result;
        }

        if (Format is not ("tree" or "mermaid" or "json"))
        {
            return ValidationResult.Error("--format must be tree, mermaid, or json.");
        }

        return Depth < 0
            ? ValidationResult.Error("--depth must be zero or greater.")
            : ValidationResult.Success();
    }
}

public sealed class DiffSettings : CommandSettings
{
    [CommandArgument(0, "<before>")]
    [Description("Baseline architecture.json file.")]
    public string Before { get; init; } = string.Empty;

    [CommandArgument(1, "<after>")]
    [Description("Current architecture.json file.")]
    public string After { get; init; } = string.Empty;

    [CommandOption("-f|--format <FORMAT>")]
    [Description("Output format: console, markdown, or json.")]
    public string Format { get; init; } = "console";

    [CommandOption("-o|--output <FILE>")]
    [Description("Write the diff to a file instead of stdout.")]
    public string? Output { get; init; }

    [CommandOption("--fail-on-new")]
    [Description("Exit with code 1 when the diff introduces new errors.")]
    public bool FailOnNew { get; init; }

    public override ValidationResult Validate()
    {
        if (!File.Exists(Before))
        {
            return ValidationResult.Error($"Baseline '{Before}' does not exist.");
        }

        if (!File.Exists(After))
        {
            return ValidationResult.Error($"Current scan '{After}' does not exist.");
        }

        return Format is "console" or "markdown" or "json"
            ? ValidationResult.Success()
            : ValidationResult.Error("--format must be console, markdown, or json.");
    }
}

public sealed class InitSettings : CommandSettings
{
    [CommandArgument(0, "[path]")]
    [Description("Repository root. Defaults to the current directory.")]
    public string Path { get; init; } = ".";

    [CommandOption("--force")]
    [Description("Overwrite an existing .archpulse/rules.json.")]
    public bool Force { get; init; }

    public override ValidationResult Validate() =>
        Directory.Exists(Path)
            ? ValidationResult.Success()
            : ValidationResult.Error($"Directory '{Path}' does not exist.");
}

public sealed class RulesSettings : CommandSettings
{
}

public sealed class ExplainSettings : CommandSettings
{
    [CommandArgument(0, "<rule>")]
    [Description("Rule id, for example AP002.")]
    public string Rule { get; init; } = string.Empty;
}

public sealed class WatchSettings : RepositorySettings
{
    [CommandOption("--fail-on <LEVEL>")]
    [Description("Exit code threshold used for each check. Defaults to error.")]
    public string FailOn { get; init; } = "error";

    public override ValidationResult Validate()
    {
        var result = base.Validate();
        if (!result.Successful)
        {
            return result;
        }

        return CliParsing.IsSeverity(FailOn)
            ? ValidationResult.Success()
            : ValidationResult.Error("--fail-on must be none, info, warning, error, or critical.");
    }
}
