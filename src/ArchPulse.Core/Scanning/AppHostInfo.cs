using ArchPulse.Core.Model;

namespace ArchPulse.Core.Scanning;

public sealed record AppHostResource(
    string Name,
    ComponentKind Kind,
    string? ProjectName = null,
    string? Evidence = null);

public sealed record AppHostReference(
    string From,
    string To,
    string? Evidence = null);

/// <summary>
/// The resources and references declared by an Aspire AppHost.
/// </summary>
public sealed record AppHostInfo(
    string ProjectName,
    IReadOnlyList<AppHostResource> Resources,
    IReadOnlyList<AppHostReference> References);
