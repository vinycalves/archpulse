using ArchPulse.Core.Model;
using ArchPulse.Core.Scanning;

namespace ArchPulse.Core.Tests;

public sealed class ProjectDiscoveryTests
{
    [Fact]
    public void FindsProjectsAndProjectReferences()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/Sample.Domain",
            "Sample.Domain",
            "Microsoft.NET.Sdk");

        repository.WriteProject(
            "src/Sample.Infrastructure",
            "Sample.Infrastructure",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\Sample.Domain\\Sample.Domain.csproj"]);

        repository.WriteProject(
            "src/Sample.Api",
            "Sample.Api",
            "Microsoft.NET.Sdk.Web",
            projectReferences:
            [
                "..\\Sample.Domain\\Sample.Domain.csproj",
                "..\\Sample.Infrastructure\\Sample.Infrastructure.csproj"
            ]);

        var projects = ProjectDiscovery.Discover(repository.Root);

        Assert.Equal(3, projects.Count);
        Assert.Contains(projects, project => project.Name == "Sample.Api" && project.Sdk.Contains(".Web"));
        Assert.Contains(projects, project => project.Name == "Sample.Infrastructure");
        Assert.Contains(
            projects.Single(project => project.Name == "Sample.Api").ProjectReferencePaths,
            reference => reference.EndsWith("Sample.Infrastructure.csproj", StringComparison.Ordinal));
    }

    [Fact]
    public void DetectsTestProjectsFromNameAndPackage()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "tests/Sample.Tests",
            "Sample.Tests",
            "Microsoft.NET.Sdk",
            packageReferences: ["Microsoft.NET.Test.Sdk"]);

        var projects = ProjectDiscovery.Discover(repository.Root);

        Assert.True(Assert.Single(projects).IsTest);
    }

    [Fact]
    public void IgnoresBuildAndSampleDirectories()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/Real",
            "Real",
            "Microsoft.NET.Sdk");

        repository.WriteProject(
            "bin/Generated",
            "Generated",
            "Microsoft.NET.Sdk");

        repository.WriteProject(
            "samples/Demo",
            "Demo",
            "Microsoft.NET.Sdk");

        var projects = ProjectDiscovery.Discover(repository.Root);

        Assert.Equal("Real", Assert.Single(projects).Name);
    }

    [Fact]
    public void ScansGraphAndAppHostReferences()
    {
        using var repository = new TestRepository();

        repository.WriteProject("src/Sample.Domain", "Sample.Domain", "Microsoft.NET.Sdk");
        repository.WriteProject(
            "src/Sample.Infrastructure",
            "Sample.Infrastructure",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\Sample.Domain\\Sample.Domain.csproj"]);
        repository.WriteProject(
            "src/Sample.Api",
            "Sample.Api",
            "Microsoft.NET.Sdk.Web",
            projectReferences:
            [
                "..\\Sample.Domain\\Sample.Domain.csproj",
                "..\\Sample.Infrastructure\\Sample.Infrastructure.csproj"
            ]);
        repository.WriteProject(
            "src/Sample.AppHost",
            "Sample.AppHost",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\Sample.Api\\Sample.Api.csproj"],
            packageReferences: ["Aspire.Hosting.AppHost"]);

        repository.Write(
            "src/Sample.AppHost/Program.cs",
            """
            var builder = DistributedApplication.CreateBuilder(args);

            var postgres = builder.AddPostgres("postgres");
            var db = postgres.AddDatabase("archpulse");
            var api = builder.AddProject<Projects.Sample_Api>("sample-api").WithReference(db);
            var infrastructure = builder.AddProject<Projects.Sample_Infrastructure>("sample-infrastructure");

            builder.Build().Run();
            """);

        var projects = ProjectDiscovery.Discover(repository.Root);
        var appHost = Assert.Single(AppHostDiscovery.Discover(projects));

        Assert.Equal("Sample.AppHost", appHost.ProjectName);
        Assert.Contains(appHost.Resources, resource => resource.Name == "postgres" && resource.Kind == ComponentKind.Database);
        Assert.Contains(appHost.Resources, resource => resource.Name == "archpulse" && resource.Kind == ComponentKind.Database);
        Assert.Contains(appHost.References, reference => reference.From == "sample-api" && reference.To == "archpulse");
    }
}
