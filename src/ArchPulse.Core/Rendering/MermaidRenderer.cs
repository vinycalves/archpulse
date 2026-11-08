using System.Text;
using System.Text.RegularExpressions;
using ArchPulse.Core.Model;

namespace ArchPulse.Core.Rendering;

/// <summary>
/// Renders the architecture graph as a Mermaid flowchart.
/// </summary>
public static partial class MermaidRenderer
{
    public static string Render(ArchitectureGraph graph)
    {
        var builder = new StringBuilder();
        builder.AppendLine("flowchart LR");

        if (graph.Components.Count == 0)
        {
            builder.AppendLine("  empty[\"No components found\"]");
            return builder.ToString();
        }

        foreach (var group in graph.Components
                     .GroupBy(component => string.IsNullOrWhiteSpace(component.Layer) ? "Other" : component.Layer)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  subgraph sg_{SafeId(group.Key)}[\"{Escape(group.Key)}\"]");
            foreach (var component in group.OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine(
                    $"    {SafeId(component.Id)}[\"{Escape(component.Name)}<br/><small>{component.Kind}</small>\"]");
            }

            builder.AppendLine("  end");
        }

        foreach (var dependency in graph.Dependencies)
        {
            var arrow = dependency.Kind switch
            {
                DependencyKind.ProjectReference => "-->",
                DependencyKind.PackageReference => "-.->",
                DependencyKind.AspireReference => "==>",
                _ => "-->"
            };

            builder.AppendLine(
                $"  {SafeId(dependency.From)} {arrow}|{dependency.Kind}| {SafeId(dependency.To)}");
        }

        AppendClassDefinitions(builder);

        foreach (var component in graph.Components)
        {
            builder.AppendLine($"  class {SafeId(component.Id)} {ClassFor(component)};");
        }

        return builder.ToString();
    }

    private static void AppendClassDefinitions(StringBuilder builder)
    {
        builder.AppendLine("  classDef domain fill:#172033,stroke:#7c5cff,color:#e5edf7;");
        builder.AppendLine("  classDef application fill:#102a2a,stroke:#22d3ee,color:#e5edf7;");
        builder.AppendLine("  classDef infra fill:#2a1f14,stroke:#fbbf24,color:#e5edf7;");
        builder.AppendLine("  classDef presentation fill:#14243a,stroke:#38bdf8,color:#e5edf7;");
        builder.AppendLine("  classDef runtime fill:#22152e,stroke:#c084fc,color:#e5edf7;");
        builder.AppendLine("  classDef orchestration fill:#2a1616,stroke:#fb7185,color:#e5edf7;");
        builder.AppendLine("  classDef external fill:#111827,stroke:#64748b,color:#cbd5e1;");
        builder.AppendLine("  classDef tests fill:#111827,stroke:#34d399,color:#cbd5e1;");
        builder.AppendLine("  classDef other fill:#111827,stroke:#475569,color:#cbd5e1;");
    }

    private static string ClassFor(ArchitectureComponent component) =>
        component.Layer switch
        {
            "Domain" => "domain",
            "Application" => "application",
            "Infrastructure" => "infra",
            "Presentation" => "presentation",
            "Runtime" => "runtime",
            "Orchestration" => "orchestration",
            "Tests" => "tests",
            "External" => "external",
            _ => "other"
        };

    private static string Escape(string value) =>
        value.Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static string SafeId(string value)
    {
        var safe = NonAlphaNumeric().Replace(value, "_");
        return $"n_{safe}";
    }

    [GeneratedRegex("[^a-zA-Z0-9_]", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphaNumeric();
}
