using ContosoShop.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<OrderRepository>();
builder.Services.AddHostedService<OrderWorker>();

var host = builder.Build();
host.Run();

internal sealed class OrderWorker(ILogger<OrderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Processing orders...");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
