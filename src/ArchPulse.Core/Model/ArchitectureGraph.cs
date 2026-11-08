namespace ArchPulse.Core.Model;

/// <summary>
/// The discovered architecture: components and the dependencies between them.
/// </summary>
public sealed record ArchitectureGraph(
    IReadOnlyList<ArchitectureComponent> Components,
    IReadOnlyList<ArchitectureDependency> Dependencies)
{
    public static ArchitectureGraph Empty { get; } = new([], []);

    public ArchitectureComponent? FindComponent(string id) =>
        Components.FirstOrDefault(component => string.Equals(component.Id, id, StringComparison.OrdinalIgnoreCase));
}
