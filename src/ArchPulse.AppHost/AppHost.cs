var builder = DistributedApplication.CreateBuilder(args);

var repoRoot = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var samplePath = Path.Combine(repoRoot, "samples", "ContosoShop");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("archpulse-postgres")
    .WithLifetime(ContainerLifetime.Persistent);
var database = postgres.AddDatabase("archpulse");

var api = builder.AddProject<Projects.ArchPulse_Api>("archpulse-api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Demo__SamplePath", samplePath)
    .WithEnvironment("Demo__SampleDisplayPath", "samples/ContosoShop")
    .WithEnvironment("Scanner__AllowedRoots__0", repoRoot)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.ArchPulse_Worker>("archpulse-worker")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Scanner__AllowedRoots__0", repoRoot);

builder.AddProject<Projects.ArchPulse_Web>("archpulse-web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
