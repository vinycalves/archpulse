using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchPulse.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 12;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ArchPulseDbContext>();
                await db.Database.EnsureCreatedAsync(cancellationToken);
                logger.LogInformation("ArchPulse database is ready.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Database is not ready (attempt {Attempt}/{MaxAttempts}). Retrying in 2 seconds...",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
