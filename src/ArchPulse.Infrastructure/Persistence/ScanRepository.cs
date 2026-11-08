using ArchPulse.Core.Model;
using ArchPulse.Core.Rendering;
using ArchPulse.Core.Serialization;
using Microsoft.EntityFrameworkCore;

namespace ArchPulse.Infrastructure.Persistence;

public sealed class ScanRepository(ArchPulseDbContext db)
{
    public Task<ScanJob?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Scans.AsNoTracking().FirstOrDefaultAsync(scan => scan.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ScanJob>> ListAsync(
        int take = 20,
        CancellationToken cancellationToken = default) =>
        await db.Scans
            .AsNoTracking()
            .OrderByDescending(scan => scan.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task<ScanJob> CreateAsync(
        string sourceType,
        string sourceValue,
        CancellationToken cancellationToken = default)
    {
        var job = new ScanJob
        {
            Id = Guid.NewGuid(),
            SourceType = sourceType,
            SourceValue = sourceValue,
            Status = ScanStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Scans.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return job;
    }

    /// <summary>
    /// Claims the next queued scan. The transaction protects the state transition;
    /// a production deployment with multiple workers should use a database-specific
    /// lock (for example FOR UPDATE SKIP LOCKED on PostgreSQL).
    /// </summary>
    public async Task<ScanJob?> ClaimNextAsync(CancellationToken cancellationToken = default)
    {
        // Npgsql's retrying execution strategy does not allow a user-initiated
        // transaction unless the whole unit is executed through the strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var job = await db.Scans
                .Where(scan => scan.Status == ScanStatus.Queued)
                .OrderBy(scan => scan.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (job is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            job.Status = ScanStatus.Running;
            job.StartedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return job;
        });
    }

    public async Task CompleteAsync(
        Guid id,
        ScanOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        var job = await db.Scans.FirstOrDefaultAsync(scan => scan.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Scan '{id}' was not found.");

        job.Status = ScanStatus.Completed;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.Error = null;
        job.GraphJson = ArchPulseJson.Serialize(outcome.Graph);
        job.ViolationsJson = ArchPulseJson.Serialize(outcome.Violations);
        job.SummaryJson = ArchPulseJson.Serialize(outcome.Summary);
        job.Mermaid = MermaidRenderer.Render(outcome.Graph);
        job.ReportMarkdown = MarkdownReportRenderer.Render(outcome, job.SourceValue, job.CompletedAt.Value);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(
        Guid id,
        string error,
        CancellationToken cancellationToken = default)
    {
        var job = await db.Scans.FirstOrDefaultAsync(scan => scan.Id == id, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Status = ScanStatus.Failed;
        job.CompletedAt = DateTimeOffset.UtcNow;
        job.Error = error.Length > 4000 ? error[..4000] : error;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await db.Scans.FirstOrDefaultAsync(scan => scan.Id == id, cancellationToken);
        if (job is null)
        {
            return false;
        }

        db.Scans.Remove(job);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
