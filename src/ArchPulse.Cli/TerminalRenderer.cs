using ArchPulse.Core.Diff;
using ArchPulse.Core.Model;
using ArchPulse.Core.Rules;
using Spectre.Console;

namespace ArchPulse.Cli;

internal static class TerminalRenderer
{
    public static IAnsiConsole ErrorConsole { get; } = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error)
    });

    public static void WriteScan(ScanOutcome outcome, string source, bool details)
    {
        AnsiConsole.Write(
            new Rule($"[bold]ArchPulse[/] [grey]{Markup.Escape(source)}[/]")
                .RuleStyle("grey"));

        WriteSummary(outcome);
        WriteComponents(outcome.Graph, details);
        WriteFindings(outcome.Violations);
    }

    public static void WriteSummary(ScanOutcome outcome)
    {
        var summary = outcome.Summary;
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[grey]Metric[/]").NoWrap())
            .AddColumn(new TableColumn("[grey]Value[/]").RightAligned());

        table.AddRow("Components", summary.Components.ToString());
        table.AddRow("Dependencies", summary.Dependencies.ToString());
        table.AddRow("Services", summary.Services.ToString());
        table.AddRow("Violations", summary.Violations.ToString());
        table.AddRow("Errors", summary.Errors.ToString());
        table.AddRow("Warnings", summary.Warnings.ToString());
        table.AddRow("Cycles", summary.Cycles.ToString());

        AnsiConsole.Write(table);
    }

    public static void WriteComponents(ArchitectureGraph graph, bool details)
    {
        var tree = new Tree("[bold]Components[/]");

        foreach (var group in graph.Components
                     .Where(component => component.Kind is not (
                         ComponentKind.External
                         or ComponentKind.Database
                         or ComponentKind.Cache
                         or ComponentKind.Broker))
                     .GroupBy(component => component.Layer)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var layerNode = tree.AddNode($"[bold]{Markup.Escape(group.Key)}[/]");

            foreach (var component in group.OrderBy(
                         component => component.Name,
                         StringComparer.OrdinalIgnoreCase))
            {
                var label = $"{Markup.Escape(component.Name)} [grey]({component.Kind})[/]";

                if (details && component.Metadata is not null)
                {
                    var framework = component.Metadata.GetValueOrDefault("targetFramework");
                    if (!string.IsNullOrWhiteSpace(framework))
                    {
                        label += $" [grey]{Markup.Escape(framework)}[/]";
                    }
                }

                layerNode.AddNode(label);
            }
        }

        AnsiConsole.Write(tree);
    }

    public static void WriteFindings(IReadOnlyList<ArchitectureViolation> violations)
    {
        if (violations.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]✔ No architecture violations found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[grey]Rule[/]").NoWrap())
            .AddColumn(new TableColumn("[grey]Severity[/]").NoWrap())
            .AddColumn("[grey]Finding[/]");

        foreach (var violation in violations)
        {
            var color = SeverityColor(violation.Severity);
            table.AddRow(
                $"[grey]{Markup.Escape(violation.RuleId)}[/]",
                $"[{color}]{violation.Severity}[/]",
                $"[bold]{Markup.Escape(violation.Title)}[/]\n[grey]{Markup.Escape(violation.Message)}[/]");
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Run `archpulse explain <rule>` for the rationale behind a finding.[/]");
    }

    public static void WriteCheck(ScanOutcome outcome, string source)
    {
        var errors = outcome.Summary.Errors;
        var warnings = outcome.Summary.Warnings;
        var status = errors > 0
            ? "[red]✖[/]"
            : warnings > 0
                ? "[yellow]![/]"
                : "[green]✔[/]";

        AnsiConsole.MarkupLine(
            $"{status} [bold]{Markup.Escape(source)}[/] — "
            + $"{outcome.Summary.Components} components, "
            + $"{errors} errors, {warnings} warnings");

        foreach (var violation in outcome.Violations)
        {
            var color = SeverityColor(violation.Severity);
            AnsiConsole.MarkupLine(
                $"  [{color}]{Markup.Escape(violation.RuleId)}[/] "
                + $"[grey]{violation.Severity}[/] "
                + $"{Markup.Escape(violation.Message)}");
        }

        if (outcome.Violations.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Architecture boundaries are healthy.[/]");
        }
    }

    public static void WriteGraph(
        ArchitectureGraph graph,
        int depth,
        bool includeExternal)
    {
        var components = graph.Components
            .Where(component => includeExternal || component.Kind is not (
                ComponentKind.External
                or ComponentKind.Database
                or ComponentKind.Cache
                or ComponentKind.Broker))
            .ToDictionary(component => component.Id, StringComparer.OrdinalIgnoreCase);

        var edges = graph.Dependencies
            .Where(dependency => components.ContainsKey(dependency.From)
                && components.ContainsKey(dependency.To))
            .GroupBy(dependency => dependency.From, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(dependency => dependency.To)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var targets = edges.Values
            .SelectMany(children => children)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var roots = components.Values
            .Where(component => !targets.Contains(component.Id))
            .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tree = new Tree("[bold]Dependency graph[/]");
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (roots.Length == 0)
        {
            roots = components.Values
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        foreach (var root in roots)
        {
            AddGraphNode(tree, root, components, edges, visited, depth, 0);
        }

        AnsiConsole.Write(tree);
    }

    private static void AddGraphNode(
        IHasTreeNodes parent,
        ArchitectureComponent component,
        IReadOnlyDictionary<string, ArchitectureComponent> components,
        IReadOnlyDictionary<string, string[]> edges,
        ISet<string> visited,
        int depth,
        int currentDepth)
    {
        var label = $"{Markup.Escape(component.Name)} [grey]({component.Kind})[/]";
        var node = parent.AddNode(label);

        if (depth > 0 && currentDepth >= depth)
        {
            return;
        }

        if (!edges.TryGetValue(component.Id, out var children))
        {
            return;
        }

        foreach (var childId in children)
        {
            if (!components.TryGetValue(childId, out var child))
            {
                continue;
            }

            if (!visited.Add(childId))
            {
                node.AddNode($"[grey]{Markup.Escape(child.Name)} (already shown)[/]");
                continue;
            }

            AddGraphNode(node, child, components, edges, visited, depth, currentDepth + 1);
        }
    }

    public static void WriteDiff(
        ArchitectureDiff diff,
        ScanOutcome before,
        ScanOutcome after,
        string beforeLabel,
        string afterLabel)
    {
        AnsiConsole.Write(
            new Rule($"[bold]Architecture diff[/] [grey]{Markup.Escape(beforeLabel)} → {Markup.Escape(afterLabel)}[/]")
                .RuleStyle("grey"));

        if (!diff.HasChanges)
        {
            AnsiConsole.MarkupLine("[green]✔ No architectural changes detected.[/]");
            return;
        }

        AnsiConsole.MarkupLine(
            $"[bold]+{diff.AddedComponents.Count}[/] / [bold]-{diff.RemovedComponents.Count}[/] components · "
            + $"[bold]+{diff.AddedDependencies.Count}[/] / [bold]-{diff.RemovedDependencies.Count}[/] dependencies · "
            + $"[bold]{diff.NewViolations.Count}[/] new / [bold]{diff.ResolvedViolations.Count}[/] resolved violations");

        var names = before.Graph.Components
            .Concat(after.Graph.Components)
            .GroupBy(component => component.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Name,
                StringComparer.OrdinalIgnoreCase);

        foreach (var component in diff.AddedComponents)
        {
            AnsiConsole.MarkupLine(
                $"[green]+[/] {Markup.Escape(component.Name)} [grey]({component.Kind})[/]");
        }

        foreach (var component in diff.RemovedComponents)
        {
            AnsiConsole.MarkupLine(
                $"[red]-[/] {Markup.Escape(component.Name)} [grey]({component.Kind})[/]");
        }

        foreach (var dependency in diff.AddedDependencies)
        {
            AnsiConsole.MarkupLine(
                $"[green]+[/] {Markup.Escape(NameOf(names, dependency.From))} → "
                + $"{Markup.Escape(NameOf(names, dependency.To))} [grey]({dependency.Kind})[/]");
        }

        foreach (var dependency in diff.RemovedDependencies)
        {
            AnsiConsole.MarkupLine(
                $"[red]-[/] {Markup.Escape(NameOf(names, dependency.From))} → "
                + $"{Markup.Escape(NameOf(names, dependency.To))} [grey]({dependency.Kind})[/]");
        }

        foreach (var violation in diff.NewViolations)
        {
            AnsiConsole.MarkupLine(
                $"[red]✖[/] {Markup.Escape(violation.RuleId)} "
                + $"{Markup.Escape(violation.Message)}");
        }

        foreach (var violation in diff.ResolvedViolations)
        {
            AnsiConsole.MarkupLine(
                $"[green]✔[/] {Markup.Escape(violation.RuleId)} "
                + $"{Markup.Escape(violation.Message)}");
        }
    }

    public static void WriteRules()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[grey]Rule[/]").NoWrap())
            .AddColumn(new TableColumn("[grey]Severity[/]").NoWrap())
            .AddColumn("[grey]Title[/]")
            .AddColumn("[grey]Description[/]");

        foreach (var rule in RuleCatalog.All)
        {
            table.AddRow(
                $"[bold]{Markup.Escape(rule.Id)}[/]",
                $"[{SeverityColor(rule.Severity)}]{rule.Severity}[/]",
                Markup.Escape(rule.Title),
                Markup.Escape(rule.Description));
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[grey]Run `archpulse explain <rule>` for context and rationale.[/]");
    }

    public static void WriteRule(RuleDefinition rule)
    {
        var panel = new Panel(
            $"[bold]{Markup.Escape(rule.Id)}[/] — {Markup.Escape(rule.Title)}\n\n"
            + $"[grey]Severity:[/] {rule.Severity}\n\n"
            + $"{Markup.Escape(rule.Description)}\n\n"
            + $"[grey]Why it matters[/]\n{Markup.Escape(rule.Rationale)}")
        {
            Header = new PanelHeader("Rule"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }

    public static void WriteInit(string path)
    {
        AnsiConsole.MarkupLine($"[green]✔ Created[/] [grey]{Markup.Escape(path)}[/]");
        AnsiConsole.MarkupLine("[grey]Edit the forbidden dependencies, then run `archpulse check .`.[/]");
    }

    public static int Error(string message, int exitCode = 2)
    {
        ErrorConsole.MarkupLine($"[red]error:[/] {Markup.Escape(message)}");
        return exitCode;
    }

    private static string SeverityColor(ViolationSeverity severity) =>
        severity switch
        {
            ViolationSeverity.Critical or ViolationSeverity.Error => "red",
            ViolationSeverity.Warning => "yellow",
            _ => "blue"
        };

    private static string NameOf(
        IReadOnlyDictionary<string, string> names,
        string id) =>
        names.TryGetValue(id, out var name) ? name : id;
}
