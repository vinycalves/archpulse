namespace ArchPulse.Core.Model;

/// <summary>
/// A directed edge in the living architecture graph.
/// </summary>
public sealed record ArchitectureDependency(
    string From,
    string To,
    DependencyKind Kind,
    string? Evidence = null);
