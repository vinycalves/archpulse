namespace ArchPulse.Core.Model;

/// <summary>
/// The full result of one repository scan.
/// </summary>
public sealed record ScanOutcome(
    ArchitectureGraph Graph,
    IReadOnlyList<ArchitectureViolation> Violations,
    ArchitectureSummary Summary);
