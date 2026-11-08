using Microsoft.EntityFrameworkCore;

namespace ArchPulse.Infrastructure.Persistence;

public sealed class ArchPulseDbContext(DbContextOptions<ArchPulseDbContext> options)
    : DbContext(options)
{
    public DbSet<ScanJob> Scans => Set<ScanJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var scan = modelBuilder.Entity<ScanJob>();

        scan.ToTable("scans");
        scan.HasKey(entity => entity.Id);
        scan.Property(entity => entity.SourceType).HasMaxLength(32).IsRequired();
        scan.Property(entity => entity.SourceValue).HasMaxLength(2048).IsRequired();
        scan.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        scan.Property(entity => entity.Error).HasMaxLength(4000);
        scan.Property(entity => entity.GraphJson).HasColumnType("text");
        scan.Property(entity => entity.ViolationsJson).HasColumnType("text");
        scan.Property(entity => entity.SummaryJson).HasColumnType("text");
        scan.Property(entity => entity.Mermaid).HasColumnType("text");
        scan.Property(entity => entity.ReportMarkdown).HasColumnType("text");
        scan.HasIndex(entity => new { entity.Status, entity.CreatedAt });
    }
}
