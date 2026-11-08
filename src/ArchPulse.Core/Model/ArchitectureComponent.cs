namespace ArchPulse.Core.Model;

/// <summary>
/// A node in the living architecture graph.
/// </summary>
public sealed record ArchitectureComponent(
    string Id,
    string Name,
    ComponentKind Kind,
    string Layer,
    string? ProjectPath = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
