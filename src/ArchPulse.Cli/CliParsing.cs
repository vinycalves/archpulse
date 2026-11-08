using ArchPulse.Core.Model;

namespace ArchPulse.Cli;

internal static class CliParsing
{
    public static bool IsSeverity(string value) =>
        value.ToLowerInvariant() is "none" or "info" or "warning" or "warn" or "error" or "critical";

    public static ViolationSeverity? ParseFailOn(
        string? value,
        ViolationSeverity? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return value.ToLowerInvariant() switch
        {
            "none" => null,
            "info" => ViolationSeverity.Info,
            "warning" or "warn" => ViolationSeverity.Warning,
            "error" => ViolationSeverity.Error,
            "critical" => ViolationSeverity.Critical,
            _ => defaultValue
        };
    }

    public static int ExitCode(
        ScanOutcome outcome,
        ViolationSeverity? threshold) =>
        threshold is not null
        && outcome.Violations.Any(violation => violation.Severity >= threshold)
            ? 1
            : 0;
}
