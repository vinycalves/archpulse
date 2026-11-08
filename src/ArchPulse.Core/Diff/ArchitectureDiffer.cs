using ArchPulse.Core.Model;

namespace ArchPulse.Core.Diff;

public static class ArchitectureDiffer
{
    public static ArchitectureDiff Compare(ScanOutcome before, ScanOutcome after)
    {
        var beforeComponents = before.Graph.Components
            .ToDictionary(component => component.Id, StringComparer.OrdinalIgnoreCase);
        var afterComponents = after.Graph.Components
            .ToDictionary(component => component.Id, StringComparer.OrdinalIgnoreCase);

        var beforeDependencies = before.Graph.Dependencies
            .Select(DependencyKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var afterDependencies = after.Graph.Dependencies
            .Select(DependencyKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var beforeViolations = before.Violations
            .Select(ViolationKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var afterViolations = after.Violations
            .Select(ViolationKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new ArchitectureDiff(
            AddedComponents: after.Graph.Components
                .Where(component => !beforeComponents.ContainsKey(component.Id))
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            RemovedComponents: before.Graph.Components
                .Where(component => !afterComponents.ContainsKey(component.Id))
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            AddedDependencies: after.Graph.Dependencies
                .Where(dependency => !beforeDependencies.Contains(DependencyKey(dependency)))
                .ToArray(),
            RemovedDependencies: before.Graph.Dependencies
                .Where(dependency => !afterDependencies.Contains(DependencyKey(dependency)))
                .ToArray(),
            NewViolations: after.Violations
                .Where(violation => !beforeViolations.Contains(ViolationKey(violation)))
                .OrderByDescending(violation => violation.Severity)
                .ToArray(),
            ResolvedViolations: before.Violations
                .Where(violation => !afterViolations.Contains(ViolationKey(violation)))
                .OrderByDescending(violation => violation.Severity)
                .ToArray());
    }

    private static string DependencyKey(ArchitectureDependency dependency) =>
        $"{dependency.From}|{dependency.To}|{dependency.Kind}";

    private static string ViolationKey(ArchitectureViolation violation) =>
        $"{violation.RuleId}|{violation.From}|{violation.To}|{violation.Title}";
}
