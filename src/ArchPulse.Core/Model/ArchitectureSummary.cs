namespace ArchPulse.Core.Model;

public sealed record ArchitectureSummary(
    int Components,
    int Dependencies,
    int Services,
    int Violations,
    int Errors,
    int Warnings,
    int Cycles)
{
    public static ArchitectureSummary From(
        ArchitectureGraph graph,
        IReadOnlyList<ArchitectureViolation> violations)
    {
        var errors = violations.Count(v => v.Severity is ViolationSeverity.Error or ViolationSeverity.Critical);
        var warnings = violations.Count(v => v.Severity == ViolationSeverity.Warning);

        return new ArchitectureSummary(
            Components: graph.Components.Count,
            Dependencies: graph.Dependencies.Count,
            Services: graph.Components.Count(c => c.Kind is ComponentKind.WebApp or ComponentKind.Worker or ComponentKind.Service),
            Violations: violations.Count,
            Errors: errors,
            Warnings: warnings,
            Cycles: violations.Count(v => v.RuleId == "AP001"));
    }
}
