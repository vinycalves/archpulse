using ArchPulse.Core.Analysis;
using ArchPulse.Core.Model;

namespace ArchPulse.Core.Tests;

public sealed class ArchitectureAnalysisTests
{
    [Fact]
    public void FlagsDomainDependingOnInfrastructure()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/Shop.Infrastructure",
            "Shop.Infrastructure",
            "Microsoft.NET.Sdk");

        repository.WriteProject(
            "src/Shop.Domain",
            "Shop.Domain",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\Shop.Infrastructure\\Shop.Infrastructure.csproj"]);

        var outcome = new RepositoryScanner().Scan(repository.Root);

        var violation = Assert.Single(outcome.Violations);
        Assert.Equal("AP002", violation.RuleId);
        Assert.Equal(ViolationSeverity.Error, violation.Severity);
        Assert.Contains("Shop.Domain", violation.Message);
        Assert.Contains("Shop.Infrastructure", violation.Message);
    }

    [Fact]
    public void FlagsProjectReferenceCycles()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/A",
            "A",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\B\\B.csproj"]);

        repository.WriteProject(
            "src/B",
            "B",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\A\\A.csproj"]);

        var outcome = new RepositoryScanner().Scan(repository.Root);

        var violation = Assert.Single(outcome.Violations, item => item.RuleId == "AP001");
        Assert.Contains("A", violation.Message);
        Assert.Contains("B", violation.Message);
        Assert.Equal(1, outcome.Summary.Cycles);
    }

    [Fact]
    public void BuildsAspireResourcesAndReferences()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/Shop.AppHost",
            "Shop.AppHost",
            "Microsoft.NET.Sdk",
            packageReferences: ["Aspire.Hosting.AppHost"]);
        repository.WriteProject("src/Shop.Api", "Shop.Api", "Microsoft.NET.Sdk.Web");

        repository.Write(
            "src/Shop.AppHost/Program.cs",
            """
            var builder = DistributedApplication.CreateBuilder(args);
            var postgres = builder.AddPostgres("postgres");
            var db = postgres.AddDatabase("shopdb");
            var api = builder.AddProject<Projects.Shop_Api>("shop-api").WithReference(db);
            builder.Build().Run();
            """);

        var outcome = new RepositoryScanner().Scan(repository.Root);

        var api = outcome.Graph.Components.Single(component => component.Name == "Shop.Api");
        var database = outcome.Graph.Components.Single(component => component.Name == "shopdb");
        var appHost = outcome.Graph.Components.Single(component => component.Name == "Shop.AppHost");

        Assert.Equal(ComponentKind.WebApp, api.Kind);
        Assert.Equal(ComponentKind.Database, database.Kind);
        Assert.Contains(
            outcome.Graph.Dependencies,
            dependency => dependency.From == api.Id
                && dependency.To == database.Id
                && dependency.Kind == DependencyKind.AspireReference);
        Assert.Contains(
            outcome.Graph.Dependencies,
            dependency => dependency.From == appHost.Id
                && dependency.To == api.Id
                && dependency.Kind == DependencyKind.AspireReference);
    }

    [Fact]
    public void LoadsCustomForbiddenDependencyRules()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/Shop.Api",
            "Shop.Api",
            "Microsoft.NET.Sdk.Web",
            projectReferences: ["..\\Shop.Domain\\Shop.Domain.csproj"]);
        repository.WriteProject("src/Shop.Domain", "Shop.Domain", "Microsoft.NET.Sdk");

        repository.Write(
            ".archpulse/rules.json",
            """
            {
              "forbiddenDependencies": [
                {
                  "from": "Shop.Api",
                  "to": "Shop.Domain",
                  "severity": "Warning",
                  "title": "API must not depend directly on the domain",
                  "message": "Use the application layer instead.",
                  "id": "SHOP-001"
                }
              ]
            }
            """);

        var outcome = new RepositoryScanner().Scan(repository.Root);

        var violation = Assert.Single(outcome.Violations, item => item.RuleId == "SHOP-001");
        Assert.Equal(ViolationSeverity.Warning, violation.Severity);
        Assert.Equal("API must not depend directly on the domain", violation.Title);
    }

    [Fact]
    public void FlagsInconsistentTargetFrameworks()
    {
        using var repository = new TestRepository();

        repository.WriteProject("src/Modern", "Modern", "Microsoft.NET.Sdk");
        repository.WriteProject(
            "src/Legacy",
            "Legacy",
            "Microsoft.NET.Sdk",
            targetFramework: "net8.0");

        var outcome = new RepositoryScanner().Scan(repository.Root);

        var violation = Assert.Single(outcome.Violations, item => item.RuleId == "AP007");
        Assert.Equal(ViolationSeverity.Warning, violation.Severity);
        Assert.Contains("Legacy", violation.Message);
        Assert.Contains("net8.0", violation.Message);
    }

    [Fact]
    public void SummaryCountsServicesAndViolations()
    {
        using var repository = new TestRepository();

        repository.WriteProject(
            "src/Shop.Infrastructure",
            "Shop.Infrastructure",
            "Microsoft.NET.Sdk");
        repository.WriteProject(
            "src/Shop.Domain",
            "Shop.Domain",
            "Microsoft.NET.Sdk",
            projectReferences: ["..\\Shop.Infrastructure\\Shop.Infrastructure.csproj"]);
        repository.WriteProject("src/Shop.Api", "Shop.Api", "Microsoft.NET.Sdk.Web");

        var outcome = new RepositoryScanner().Scan(repository.Root);

        Assert.Equal(3, outcome.Summary.Components);
        Assert.Equal(1, outcome.Summary.Services);
        Assert.Equal(1, outcome.Summary.Errors);
        Assert.Equal(1, outcome.Summary.Violations);
    }
}
