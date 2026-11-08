using ArchPulse.Core.Model;
using ArchPulse.Core.Scanning;

namespace ArchPulse.Core.Analysis;

/// <summary>
/// Turns project files and AppHost declarations into one architecture graph.
/// </summary>
public sealed class ArchitectureGraphBuilder
{
    private static readonly (string Prefix, string Display, ComponentKind Kind)[] KnownPackages =
    [
        ("Microsoft.EntityFrameworkCore", "EF Core", ComponentKind.External),
        ("Npgsql", "PostgreSQL", ComponentKind.Database),
        ("Microsoft.Data.SqlClient", "SQL Server", ComponentKind.Database),
        ("MongoDB.Driver", "MongoDB", ComponentKind.Database),
        ("Dapper", "Dapper", ComponentKind.External),
        ("StackExchange.Redis", "Redis", ComponentKind.Cache),
        ("RabbitMQ.Client", "RabbitMQ", ComponentKind.Broker),
        ("MassTransit", "MassTransit", ComponentKind.Broker),
        ("Confluent.Kafka", "Kafka", ComponentKind.Broker),
        ("Azure.Messaging", "Azure Messaging", ComponentKind.Broker),
        ("Yarp.ReverseProxy", "YARP", ComponentKind.External),
        ("OpenTelemetry", "OpenTelemetry", ComponentKind.External),
        ("MediatR", "MediatR", ComponentKind.External),
        ("FluentValidation", "FluentValidation", ComponentKind.External),
        ("Polly", "Polly", ComponentKind.External),
        ("Serilog", "Serilog", ComponentKind.External)
    ];

    public ArchitectureGraph Build(
        IReadOnlyList<ProjectInfo> projects,
        IReadOnlyList<AppHostInfo> appHosts)
    {
        var components = new Dictionary<string, ArchitectureComponent>(StringComparer.OrdinalIgnoreCase);
        var edges = new List<ArchitectureDependency>();
        var edgeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var projectByPath = projects.ToDictionary(project => project.FullPath, StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects)
        {
            var component = CreateComponent(project);
            components[component.Id] = component;
        }

        foreach (var project in projects)
        {
            var from = ComponentId.ForProject(project.Name);

            foreach (var referencePath in project.ProjectReferencePaths)
            {
                if (!projectByPath.TryGetValue(referencePath, out var target))
                {
                    continue;
                }

                AddEdge(
                    from,
                    ComponentId.ForProject(target.Name),
                    DependencyKind.ProjectReference,
                    target.RelativePath);
            }

            foreach (var package in project.PackageReferences)
            {
                var known = KnownPackages.FirstOrDefault(candidate =>
                    package.StartsWith(candidate.Prefix, StringComparison.OrdinalIgnoreCase));

                if (known.Display is null)
                {
                    continue;
                }

                var externalId = ComponentId.ForExternal(known.Display);
                components.TryAdd(
                    externalId,
                    new ArchitectureComponent(
                        externalId,
                        known.Display,
                        known.Kind,
                        "External",
                        Metadata: new Dictionary<string, string>
                        {
                            ["package"] = package,
                            ["technology"] = known.Display
                        }));

                AddEdge(from, externalId, DependencyKind.PackageReference, package);
            }
        }

        foreach (var appHost in appHosts)
        {
            var appHostProject = projects.FirstOrDefault(project =>
                project.Name.Equals(appHost.ProjectName, StringComparison.OrdinalIgnoreCase));
            var appHostId = appHostProject is null
                ? ComponentId.ForExternal(appHost.ProjectName)
                : ComponentId.ForProject(appHostProject.Name);

            components.TryAdd(
                appHostId,
                new ArchitectureComponent(
                    appHostId,
                    appHost.ProjectName,
                    ComponentKind.AppHost,
                    "Orchestration"));

            var resourceToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var resource in appHost.Resources)
            {
                var project = FindProjectForResource(resource, projects);
                if (project is not null)
                {
                    resourceToId[resource.Name] = ComponentId.ForProject(project.Name);
                    AddEdge(
                        appHostId,
                        ComponentId.ForProject(project.Name),
                        DependencyKind.AspireReference,
                        resource.Evidence);
                    continue;
                }

                var externalId = ComponentId.ForResource(resource.Name);
                resourceToId[resource.Name] = externalId;
                components.TryAdd(
                    externalId,
                    new ArchitectureComponent(
                        externalId,
                        resource.Name,
                        resource.Kind,
                        "Infrastructure",
                        Metadata: new Dictionary<string, string>
                        {
                            ["aspireResource"] = "true"
                        }));
            }

            foreach (var reference in appHost.References)
            {
                if (!resourceToId.TryGetValue(reference.From, out var fromId))
                {
                    var fromProject = FindProjectByNameOrResource(reference.From, projects);
                    fromId = fromProject is not null
                        ? ComponentId.ForProject(fromProject.Name)
                        : ComponentId.ForResource(reference.From);

                    components.TryAdd(
                        fromId,
                        new ArchitectureComponent(fromId, reference.From, ComponentKind.External, "External"));
                }

                if (!resourceToId.TryGetValue(reference.To, out var toId))
                {
                    var toProject = FindProjectByNameOrResource(reference.To, projects);
                    toId = toProject is not null
                        ? ComponentId.ForProject(toProject.Name)
                        : ComponentId.ForResource(reference.To);

                    components.TryAdd(
                        toId,
                        new ArchitectureComponent(toId, reference.To, ComponentKind.External, "External"));
                }

                AddEdge(fromId, toId, DependencyKind.AspireReference, reference.Evidence);
            }
        }

        return new ArchitectureGraph(
            components.Values
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            edges
                .OrderBy(edge => edge.From, StringComparer.OrdinalIgnoreCase)
                .ThenBy(edge => edge.To, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        void AddEdge(string from, string to, DependencyKind kind, string? evidence)
        {
            if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!edgeKeys.Add($"{from}|{to}|{kind}"))
            {
                return;
            }

            edges.Add(new ArchitectureDependency(from, to, kind, evidence));
        }
    }

    private static ArchitectureComponent CreateComponent(ProjectInfo project)
    {
        var kind = KindOf(project);
        var layer = LayerOf(project, kind);

        return new ArchitectureComponent(
            Id: ComponentId.ForProject(project.Name),
            Name: project.Name,
            Kind: kind,
            Layer: layer,
            ProjectPath: project.RelativePath,
            Metadata: new Dictionary<string, string>
            {
                ["sdk"] = project.Sdk,
                ["outputType"] = project.OutputType ?? string.Empty,
                ["targetFramework"] = project.Properties.GetValueOrDefault("TargetFramework")
                    ?? project.Properties.GetValueOrDefault("TargetFrameworks")
                    ?? string.Empty,
                ["nullable"] = project.Properties.GetValueOrDefault("Nullable") ?? string.Empty,
                ["packages"] = string.Join(", ", project.PackageReferences)
            });
    }

    private static ComponentKind KindOf(ProjectInfo project)
    {
        if (project.IsTest)
        {
            return ComponentKind.Test;
        }

        if (AppHostDiscovery.IsAppHost(project))
        {
            return ComponentKind.AppHost;
        }

        if (project.Sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentKind.WebApp;
        }

        if (project.Name.Contains("Worker", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentKind.Worker;
        }

        if (project.Name.Contains("Api", StringComparison.OrdinalIgnoreCase)
            || project.Name.Contains("Web", StringComparison.OrdinalIgnoreCase))
        {
            return ComponentKind.WebApp;
        }

        if (project.OutputType?.Equals("Exe", StringComparison.OrdinalIgnoreCase) == true)
        {
            return ComponentKind.Service;
        }

        return ComponentKind.Library;
    }

    private static string LayerOf(ProjectInfo project, ComponentKind kind)
    {
        if (kind == ComponentKind.Test)
        {
            return "Tests";
        }

        if (kind == ComponentKind.AppHost)
        {
            return "Orchestration";
        }

        var probe = $"{project.Name} {project.RelativePath}";

        if (Contains(probe, ".Domain") || Contains(probe, ".Core"))
        {
            return "Domain";
        }

        if (Contains(probe, ".Application") || Contains(probe, ".UseCases") || Contains(probe, ".Features"))
        {
            return "Application";
        }

        if (Contains(probe, ".Infrastructure") || Contains(probe, ".Persistence") || Contains(probe, ".Adapters"))
        {
            return "Infrastructure";
        }

        if (kind is ComponentKind.WebApp)
        {
            return "Presentation";
        }

        if (kind is ComponentKind.Worker or ComponentKind.Service)
        {
            return "Runtime";
        }

        return "Shared";
    }

    private static bool Contains(string value, string token) =>
        value.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static ProjectInfo? FindProjectForResource(
        AppHostResource resource,
        IReadOnlyList<ProjectInfo> projects)
    {
        if (resource.ProjectName is not null)
        {
            var exact = projects.FirstOrDefault(project =>
                project.Name.Equals(resource.ProjectName, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }

            var normalized = NormalizeKey(resource.ProjectName);
            var fuzzy = projects.FirstOrDefault(project => NormalizeKey(project.Name) == normalized);
            if (fuzzy is not null)
            {
                return fuzzy;
            }
        }

        var resourceKey = NormalizeKey(resource.Name);
        return projects.FirstOrDefault(project => NormalizeKey(project.Name) == resourceKey);
    }

    private static ProjectInfo? FindProjectByNameOrResource(
        string value,
        IReadOnlyList<ProjectInfo> projects)
    {
        var normalizedName = AppHostDiscovery.NormalizeProjectName(value);
        var direct = projects.FirstOrDefault(project =>
            project.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        if (direct is not null)
        {
            return direct;
        }

        var key = NormalizeKey(value);
        return projects.FirstOrDefault(project => NormalizeKey(project.Name) == key);
    }

    private static string NormalizeKey(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
