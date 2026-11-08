namespace ArchPulse.Core.Model;

/// <summary>
/// A finding produced by the architectural rules engine.
/// </summary>
public sealed record ArchitectureViolation(
    string RuleId,
    ViolationSeverity Severity,
    string Title,
    string Message,
    string? From = null,
    string? To = null);
