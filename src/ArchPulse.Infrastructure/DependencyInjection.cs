using ArchPulse.Core.Analysis;
using ArchPulse.Infrastructure.Persistence;
using ArchPulse.Infrastructure.Scanning;
using ArchPulse.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArchPulse.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Uses the Aspire PostgreSQL resource when a connection string is present,
    /// and falls back to SQLite for standalone development and tests.
    /// </summary>
    public static IHostApplicationBuilder AddArchPulseDatabase(
        this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("archpulse");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.AddNpgsqlDbContext<ArchPulseDbContext>("archpulse");
            return builder;
        }

        var sqlitePath = Path.Combine(
            Path.GetTempPath(),
            "archpulse",
            "archpulse.db");
        Directory.CreateDirectory(Path.GetDirectoryName(sqlitePath)!);

        var sqliteConnectionString = builder.Configuration.GetConnectionString("ArchPulse")
            ?? $"Data Source={sqlitePath}";

        builder.Services.AddDbContext<ArchPulseDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        return builder;
    }

    public static IServiceCollection AddArchPulseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ScannerOptions>(configuration.GetSection("Scanner"));
        services.AddScoped<ScanRepository>();
        services.AddSingleton<RepositoryScanner>();
        services.AddSingleton<LocalPathSourceResolver>();
        services.AddSingleton<GitHubSourceResolver>();
        services.AddSingleton<ISourceResolver, CompositeSourceResolver>();
        services.AddScoped<ScanRunner>();

        return services;
    }
}
