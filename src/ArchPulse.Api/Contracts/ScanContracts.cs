using ArchPulse.Core.Model;

namespace ArchPulse.Api.Contracts;

public sealed record CreateScanRequest(string SourceType, string SourceValue);

public sealed record ScanSummaryDto(
    Guid Id,
    ScanStatus Status,
    string SourceType,
    string SourceValue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Error,
    ArchitectureSummary? Summary);

public sealed record ScanDto(
    Guid Id,
    ScanStatus Status,
    string SourceType,
    string SourceValue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Error,
    ArchitectureSummary? Summary,
    ArchitectureGraph? Graph,
    IReadOnlyList<ArchitectureViolation>? Violations,
    string? Mermaid,
    string? ReportMarkdown);
