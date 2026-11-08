using ArchPulse.Core.Model;

namespace ArchPulse.Core.Diff;

/// <summary>
/// The architectural delta between two scans.
/// </summary>
public sealed record ArchitectureDiff(
    IReadOnlyList<ArchitectureComponent> AddedComponents,
    IReadOnlyList<ArchitectureComponent> RemovedComponents,
    IReadOnlyList<ArchitectureDependency> AddedDependencies,
    IReadOnlyList<ArchitectureDependency> RemovedDependencies,
    IReadOnlyList<ArchitectureViolation> NewViolations,
    IReadOnlyList<ArchitectureViolation> ResolvedViolations)
{
    public bool HasChanges =>
        AddedComponents.Count > 0
        || RemovedComponents.Count > 0
        || AddedDependencies.Count > 0
        || RemovedDependencies.Count > 0
        || NewViolations.Count > 0
        || ResolvedViolations.Count > 0;
}
