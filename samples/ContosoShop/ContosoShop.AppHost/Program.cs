var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("contosodb");
var cache = builder.AddRedis("cache");

var api = builder.AddProject<Projects.ContosoShop_Api>("contoso-api")
    .WithReference(db)
    .WithReference(cache);

builder.AddProject<Projects.ContosoShop_Worker>("contoso-worker")
    .WithReference(db)
    .WithReference(api);

builder.Build().Run();
