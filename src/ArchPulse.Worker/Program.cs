using ArchPulse.Infrastructure;
using ArchPulse.Infrastructure.Persistence;
using ArchPulse.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddArchPulseDatabase();
builder.Services.AddArchPulseInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ScanWorker>();

var host = builder.Build();

await DatabaseInitializer.InitializeAsync(
    host.Services,
    host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Database"));

await host.RunAsync();
