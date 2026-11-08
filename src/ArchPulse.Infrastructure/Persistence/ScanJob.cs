using ArchPulse.Core.Model;

namespace ArchPulse.Infrastructure.Persistence;

/// <summary>
/// Persisted state for one architecture scan.
/// </summary>
public sealed class ScanJob
{
    public Guid Id { get; set; }

    public string SourceType { get; set; } = string.Empty;

    public string SourceValue { get; set; } = string.Empty;

    public ScanStatus Status { get; set; } = ScanStatus.Queued;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? Error { get; set; }

    public string? GraphJson { get; set; }

    public string? ViolationsJson { get; set; }

    public string? SummaryJson { get; set; }

    public string? Mermaid { get; set; }

    public string? ReportMarkdown { get; set; }
}
