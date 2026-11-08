using ArchPulse.Core.Model;

namespace ArchPulse.Core.Rules;

public sealed record RuleDefinition(
    string Id,
    string Title,
    ViolationSeverity Severity,
    string Description,
    string Rationale);

/// <summary>
/// Single source of truth for the built-in rule catalog, used by the CLI
/// and by the documentation.
/// </summary>
public static class RuleCatalog
{
    public static IReadOnlyList<RuleDefinition> All { get; } =
    [
        new(
            "AP000",
            "No .NET projects found",
            ViolationSeverity.Info,
            "The scanner did not find any .csproj files under the selected path.",
            "This usually means the path points to the wrong directory or to a non-.NET repository."),
        new(
            "AP001",
            "Project reference cycle",
            ViolationSeverity.Error,
            "A project reference cycle was found in the build graph.",
            "Cycles make the build graph fragile and usually indicate a missing abstraction or a misplaced type."),
        new(
            "AP002",
            "Layer dependency violation",
            ViolationSeverity.Error,
            "Domain or application code depends on infrastructure, presentation, runtime, or orchestration.",
            "The dependency rule keeps the center of the architecture independent from delivery and persistence details."),
        new(
            "AP003",
            "Test project referenced by production code",
            ViolationSeverity.Error,
            "A production project references a test project.",
            "Test dependencies must not leak into the product; invert the dependency or move the shared code."),
        new(
            "AP004",
            "AppHost referenced by a service",
            ViolationSeverity.Error,
            "A service project references the Aspire AppHost.",
            "The AppHost orchestrates services; it must remain a leaf so that services stay independently deployable."),
        new(
            "AP006",
            "Persistence dependency in the domain layer",
            ViolationSeverity.Warning,
            "A domain project references an ORM, database driver, cache, or broker package.",
            "Persistence belongs behind an infrastructure boundary. The domain should express intent, not storage details."),
        new(
            "AP007",
            "Inconsistent target framework",
            ViolationSeverity.Warning,
            "A project targets a different framework than the solution majority.",
            "Mixed target frameworks complicate shared libraries and CI. This rule surfaces accidental drift."),
        new(
            "AP100",
            "Forbidden dependency",
            ViolationSeverity.Error,
            "A repository-specific forbidden dependency from .archpulse/rules.json matched an edge.",
            "Custom rules encode decisions that are specific to one codebase or organization.")
    ];

    public static RuleDefinition? Find(string id) =>
        All.FirstOrDefault(rule => rule.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
