using System.Text.RegularExpressions;
using ArchPulse.Core.Model;

namespace ArchPulse.Core.Scanning;

/// <summary>
/// Reads Aspire AppHost source files and extracts the resources it declares.
/// This is intentionally a pragmatic source parser rather than a Roslyn analyzer:
/// it keeps ArchPulse usable even when the target repository cannot be built.
/// </summary>
public static partial class AppHostDiscovery
{
    private static readonly Regex ProjectRegex = new(
        @"AddProject(?:<(?<type>[^>]+)>)?\s*\(\s*""(?<name>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex ResourceRegex = new(
        @"Add(?<method>Postgres|Redis|RabbitMQ|SqlServer|MongoDB|Kafka|Nats|Container|Garnet|Minio|Keycloak)\s*\(\s*""(?<name>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex DatabaseRegex = new(
        @"\.AddDatabase\s*\(\s*""(?<name>[^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex VariableRegex = new(
        @"var\s+(?<var>\w+)\s*=\s*(?<expr>[^;]+)",
        RegexOptions.Compiled);

    private static readonly Regex WithReferenceRegex = new(
        @"(?<receiver>\w+)?\.?WithReference\s*\(\s*(?<arg>[^)]+)\)",
        RegexOptions.Compiled);

    public static IReadOnlyList<AppHostInfo> Discover(IReadOnlyList<ProjectInfo> projects)
    {
        var appHosts = new List<AppHostInfo>();

        foreach (var project in projects.Where(IsAppHost))
        {
            var source = ReadSource(project);
            appHosts.Add(Parse(project.Name, source));
        }

        return appHosts;
    }

    public static bool IsAppHost(ProjectInfo project) =>
        project.Sdk.Contains("Aspire.AppHost.Sdk", StringComparison.OrdinalIgnoreCase)
        || project.PackageReferences.Any(package =>
            package.Equals("Aspire.Hosting.AppHost", StringComparison.OrdinalIgnoreCase));

    public static string NormalizeProjectName(string value)
    {
        var name = value.Trim();
        if (name.StartsWith("global::", StringComparison.Ordinal))
        {
            name = name["global::".Length..];
        }

        if (name.StartsWith("Projects.", StringComparison.Ordinal))
        {
            name = name["Projects.".Length..];
        }

        return name.Replace('_', '.');
    }

    public static AppHostInfo Parse(string projectName, string source)
    {
        var resources = new Dictionary<string, AppHostResource>(StringComparer.OrdinalIgnoreCase);
        var references = new List<AppHostReference>();
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var statements = source.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // First pass: capture variables so later statements can resolve WithReference(db).
        foreach (var statement in statements)
        {
            var variableMatch = VariableRegex.Match(statement);
            if (!variableMatch.Success)
            {
                continue;
            }

            var variable = variableMatch.Groups["var"].Value;
            var expression = variableMatch.Groups["expr"].Value;

            var projectMatch = ProjectRegex.Match(expression);
            if (projectMatch.Success)
            {
                var resource = CreateProjectResource(projectMatch);
                resources[resource.Name] = resource;
                variables[variable] = resource.Name;
                continue;
            }

            var databaseMatch = DatabaseRegex.Match(expression);
            if (databaseMatch.Success)
            {
                var resource = new AppHostResource(databaseMatch.Groups["name"].Value, ComponentKind.Database);
                resources[resource.Name] = resource;
                variables[variable] = resource.Name;
                continue;
            }

            var resourceMatch = ResourceRegex.Match(expression);
            if (resourceMatch.Success)
            {
                var resource = CreateResource(resourceMatch);
                resources[resource.Name] = resource;
                variables[variable] = resource.Name;
            }
        }

        // Second pass: capture inline declarations and all WithReference calls.
        foreach (var statement in statements)
        {
            foreach (Match projectMatch in ProjectRegex.Matches(statement))
            {
                var resource = CreateProjectResource(projectMatch);
                resources[resource.Name] = resource;
            }

            foreach (Match resourceMatch in ResourceRegex.Matches(statement))
            {
                var resource = CreateResource(resourceMatch);
                resources[resource.Name] = resource;
            }

            foreach (Match databaseMatch in DatabaseRegex.Matches(statement))
            {
                var resource = new AppHostResource(databaseMatch.Groups["name"].Value, ComponentKind.Database);
                resources[resource.Name] = resource;
            }

            var projectInStatement = ProjectRegex.Match(statement);
            string? from = projectInStatement.Success
                ? projectInStatement.Groups["name"].Value
                : null;

            foreach (Match referenceMatch in WithReferenceRegex.Matches(statement))
            {
                from ??= ResolveReference(referenceMatch.Groups["receiver"].Value, variables);
                var to = ResolveReference(referenceMatch.Groups["arg"].Value, variables);
                if (from is null || to is null)
                {
                    continue;
                }

                references.Add(new AppHostReference(from, to, statement.Trim()));
            }
        }

        return new AppHostInfo(
            ProjectName: projectName,
            Resources: resources.Values
                .OrderBy(resource => resource.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            References: references
                .DistinctBy(reference => $"{reference.From}->{reference.To}", StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static string? ResolveReference(string value, IReadOnlyDictionary<string, string> variables)
    {
        var candidate = value.Trim().Trim('"');
        if (variables.TryGetValue(candidate, out var mapped))
        {
            return mapped;
        }

        var projectMatch = ProjectRegex.Match(candidate);
        if (projectMatch.Success)
        {
            return projectMatch.Groups["name"].Value;
        }

        var databaseMatch = DatabaseRegex.Match(candidate);
        if (databaseMatch.Success)
        {
            return databaseMatch.Groups["name"].Value;
        }

        return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
    }

    private static AppHostResource CreateProjectResource(Match match)
    {
        var type = match.Groups["type"].Success ? match.Groups["type"].Value : null;
        var name = match.Groups["name"].Value;
        return new AppHostResource(
            Name: name,
            Kind: ComponentKind.Service,
            ProjectName: type is null ? null : NormalizeProjectName(type),
            Evidence: match.Value);
    }

    private static AppHostResource CreateResource(Match match)
    {
        var method = match.Groups["method"].Value;
        var name = match.Groups["name"].Value;
        var kind = method switch
        {
            "Postgres" or "SqlServer" or "MongoDB" => ComponentKind.Database,
            "Redis" or "Garnet" => ComponentKind.Cache,
            "RabbitMQ" or "Kafka" or "Nats" => ComponentKind.Broker,
            _ => ComponentKind.External
        };

        return new AppHostResource(name, kind, Evidence: match.Value);
    }

    private static string ReadSource(ProjectInfo project)
    {
        var directory = Path.GetDirectoryName(project.FullPath)
            ?? throw new InvalidOperationException($"Cannot resolve the directory of '{project.FullPath}'.");

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
    }
}
