using ArchPulse.Core.Analysis;
using ArchPulse.Core.Model;
using ArchPulse.Core.Scanning;

namespace ArchPulse.Core.Rules;

/// <summary>
/// Evaluates built-in and repository-specific architecture rules.
/// </summary>
public sealed class ArchitectureRuleEngine(ArchitectureRules rules)
{
    private static readonly string[] ForbiddenLayers =
    [
        "Infrastructure",
        "Presentation",
        "Runtime",
        "Orchestration"
    ];

    private static readonly string[] PersistencePackages =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.Data.SqlClient",
        "MongoDB.Driver",
        "Dapper",
        "StackExchange.Redis",
        "RabbitMQ.Client",
        "MassTransit",
        "Confluent.Kafka"
    ];

    public IReadOnlyList<ArchitectureViolation> Evaluate(
        IReadOnlyList<ProjectInfo> projects,
        ArchitectureGraph graph,
        IReadOnlyList<AppHostInfo> appHosts)
    {
        var violations = new List<ArchitectureViolation>();
        var components = graph.Components.ToDictionary(
            component => component.Id,
            StringComparer.OrdinalIgnoreCase);

        if (projects.Count == 0)
        {
            Add(
                "AP000",
                ViolationSeverity.Info,
                "No .NET projects found",
                "ArchPulse did not find any .csproj files under the scanned path.");
        }

        var projectEdges = graph.Dependencies
            .Where(dependency => dependency.Kind == DependencyKind.ProjectReference)
            .ToArray();

        foreach (var dependency in projectEdges)
        {
            if (!components.TryGetValue(dependency.From, out var from)
                || !components.TryGetValue(dependency.To, out var to))
            {
                continue;
            }

            if (from.Layer == "Domain" && ForbiddenLayers.Contains(to.Layer))
            {
                Add(
                    "AP002",
                    ViolationSeverity.Error,
                    "Layer dependency violation",
                    $"{from.Name} (Domain) must not depend on {to.Name} ({to.Layer}).",
                    from.Id,
                    to.Id);
            }

            if (from.Layer == "Application" && ForbiddenLayers.Contains(to.Layer))
            {
                Add(
                    "AP002",
                    ViolationSeverity.Error,
                    "Layer dependency violation",
                    $"{from.Name} (Application) must not depend on {to.Name} ({to.Layer}).",
                    from.Id,
                    to.Id);
            }

            if (to.Kind == ComponentKind.Test && from.Kind != ComponentKind.Test)
            {
                Add(
                    "AP003",
                    ViolationSeverity.Error,
                    "Test project referenced by production code",
                    $"{from.Name} references the test project {to.Name}.",
                    from.Id,
                    to.Id);
            }

            if (to.Kind == ComponentKind.AppHost && from.Kind != ComponentKind.AppHost)
            {
                Add(
                    "AP004",
                    ViolationSeverity.Error,
                    "AppHost referenced by a service",
                    $"{from.Name} references the Aspire AppHost {to.Name}.",
                    from.Id,
                    to.Id);
            }
        }

        foreach (var cycle in FindCycles(projectEdges))
        {
            var names = cycle
                .Select(id => components.TryGetValue(id, out var component) ? component.Name : id)
                .ToArray();

            Add(
                "AP001",
                ViolationSeverity.Error,
                "Project reference cycle",
                $"Circular dependency detected: {string.Join(" -> ", names)}.",
                cycle[0],
                cycle[^1]);
        }

        foreach (var project in projects)
        {
            var id = ComponentId.ForProject(project.Name);
            if (!components.TryGetValue(id, out var component) || component.Layer != "Domain")
            {
                continue;
            }

            foreach (var package in project.PackageReferences)
            {
                if (!PersistencePackages.Any(candidate =>
                        package.StartsWith(candidate, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                Add(
                    "AP006",
                    ViolationSeverity.Warning,
                    "Persistence dependency in the domain layer",
                    $"{project.Name} is a domain project but references '{package}'. Keep persistence behind an infrastructure boundary.",
                    component.Id,
                    package);
            }
        }

        var framedComponents = graph.Components
            .Where(component => component.Metadata is not null
                && component.Metadata.TryGetValue("targetFramework", out var framework)
                && !string.IsNullOrWhiteSpace(framework))
            .Select(component => new
            {
                Component = component,
                Framework = component.Metadata!["targetFramework"]
            })
            .ToArray();

        if (framedComponents.Length > 1)
        {
            var majorityFramework = framedComponents
                .GroupBy(item => item.Framework, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .First()
                .Key;

            foreach (var item in framedComponents.Where(item =>
                         !item.Framework.Equals(majorityFramework, StringComparison.OrdinalIgnoreCase)))
            {
                Add(
                    "AP007",
                    ViolationSeverity.Warning,
                    "Inconsistent target framework",
                    $"{item.Component.Name} targets {item.Framework}, while the solution majority targets {majorityFramework}.",
                    item.Component.Id);
            }
        }

        foreach (var rule in rules.ForbiddenDependencies)
        {
            foreach (var dependency in graph.Dependencies)
            {
                if (!components.TryGetValue(dependency.From, out var from)
                    || !components.TryGetValue(dependency.To, out var to))
                {
                    continue;
                }

                if (!GlobMatcher.IsMatch(from.Name, rule.From)
                    || !GlobMatcher.IsMatch(to.Name, rule.To))
                {
                    continue;
                }

                Add(
                    rule.Id,
                    rule.Severity,
                    rule.Title ?? "Forbidden dependency",
                    rule.Message ?? $"{from.Name} must not depend on {to.Name}.",
                    from.Id,
                    to.Id);
            }
        }

        return violations
            .GroupBy(
                violation => $"{violation.RuleId}|{violation.From}|{violation.To}|{violation.Title}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(violation => violation.Severity).First())
            .OrderByDescending(violation => violation.Severity)
            .ThenBy(violation => violation.RuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(violation => violation.From, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        void Add(
            string ruleId,
            ViolationSeverity severity,
            string title,
            string message,
            string? from = null,
            string? to = null)
        {
            violations.Add(new ArchitectureViolation(ruleId, severity, title, message, from, to));
        }
    }

    private static IReadOnlyList<string[]> FindCycles(
        IReadOnlyList<ArchitectureDependency> edges)
    {
        var adjacency = edges
            .GroupBy(edge => edge.From, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => edge.To)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();
        var cycles = new List<string[]>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in adjacency.Keys)
        {
            if (!state.ContainsKey(node))
            {
                Visit(node);
            }
        }

        return cycles;

        void Visit(string node)
        {
            state[node] = 1;
            stack.Add(node);

            var neighbors = adjacency.TryGetValue(node, out var value) ? value : [];
            foreach (var next in neighbors)
            {
                if (!state.TryGetValue(next, out var nextState))
                {
                    Visit(next);
                }
                else if (nextState == 1)
                {
                    var index = stack.FindIndex(id =>
                        id.Equals(next, StringComparison.OrdinalIgnoreCase));

                    if (index < 0)
                    {
                        continue;
                    }

                    var cycle = stack.Skip(index).Append(next).ToArray();
                    var key = string.Join(
                        "|",
                        cycle.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));

                    if (seen.Add(key))
                    {
                        cycles.Add(cycle);
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[node] = 2;
        }
    }
}
