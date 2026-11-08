using System.Text.Json.Serialization;
using ArchPulse.Api.Endpoints;
using ArchPulse.Infrastructure;
using ArchPulse.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddArchPulseDatabase();
builder.Services.AddArchPulseInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapScanEndpoints();
app.MapGet("/api/meta", () => new
{
    name = "ArchPulse",
    version = "0.1.0",
    description = "Living architecture for .NET and Aspire applications."
});

await DatabaseInitializer.InitializeAsync(
    app.Services,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Database"));

app.Run();
