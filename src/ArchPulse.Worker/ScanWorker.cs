using ArchPulse.Infrastructure.Persistence;
using ArchPulse.Infrastructure.Scanning;
using ArchPulse.Infrastructure.Sources;

namespace ArchPulse.Worker;

public sealed class ScanWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ScanWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ScanRepository>();
                var runner = scope.ServiceProvider.GetRequiredService<ScanRunner>();

                var job = await repository.ClaimNextAsync(stoppingToken);
                if (job is null)
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                    continue;
                }

                logger.LogInformation(
                    "Scan {ScanId} started for {SourceType}: {SourceValue}.",
                    job.Id,
                    job.SourceType,
                    job.SourceValue);

                try
                {
                    var outcome = await runner.RunAsync(
                        new ScanSource(job.SourceType, job.SourceValue),
                        stoppingToken);

                    await repository.CompleteAsync(job.Id, outcome, stoppingToken);

                    logger.LogInformation(
                        "Scan {ScanId} completed with {Components} components and {Violations} violations.",
                        job.Id,
                        outcome.Summary.Components,
                        outcome.Summary.Violations);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Scan {ScanId} failed.", job.Id);
                    await repository.FailAsync(job.Id, exception.Message, CancellationToken.None);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The scan worker loop failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
