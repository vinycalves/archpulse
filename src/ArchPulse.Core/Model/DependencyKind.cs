namespace ArchPulse.Core.Model;

/// <summary>
/// How one component depends on another.
/// </summary>
public enum DependencyKind
{
    ProjectReference,
    PackageReference,
    AspireReference,
    Http
}
