using System.Text.Json;
using ArchPulse.Core.Model;
using ArchPulse.Core.Serialization;

namespace ArchPulse.Core.Rules;

public sealed record ForbiddenDependencyRule(
    string From,
    string To,
    ViolationSeverity Severity = ViolationSeverity.Error,
    string? Title = null,
    string? Message = null,
    string Id = "AP100");

/// <summary>
/// Repository-specific architecture rules loaded from .archpulse/rules.json.
/// </summary>
public sealed class ArchitectureRules
{
    public IReadOnlyList<ForbiddenDependencyRule> ForbiddenDependencies { get; init; } = [];

    public static ArchitectureRules Default { get; } = new();

    public static ArchitectureRules Load(string rootPath)
    {
        var path = Path.Combine(rootPath, ".archpulse", "rules.json");
        if (!File.Exists(path))
        {
            return Default;
        }

        try
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize<ArchitectureRulesFile>(json, ArchPulseJson.Options);
            return new ArchitectureRules
            {
                ForbiddenDependencies = file?.ForbiddenDependencies ?? []
            };
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Could not load architecture rules from '{path}': {exception.Message}",
                exception);
        }
    }

    private sealed record ArchitectureRulesFile(
        IReadOnlyList<ForbiddenDependencyRule>? ForbiddenDependencies);
}
